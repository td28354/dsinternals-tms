namespace DSInternals.DataStore
{
    using DSInternals.Common;
    using DSInternals.Common.Data;
    using DSInternals.Common.Exceptions;
    using Microsoft.Database.Isam;
    using Microsoft.Isam.Esent.Interop;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The ActiveDirectorySchema class represents the schema partition for a particular domain.
    /// </summary>
    public class DirectorySchema
    {
        private const string AttributeColPrefix = "ATT";
        private const string AttributeColIndexPrefix = "INDEX_";
        private const string SystemColSuffix = "_col";
        private const string SystemColIndexSuffix = "_index";
        private const char IndexNameComponentSeparator = '_';
        private const string ParentDNTagIndex = "PDNT_index";

        private IDictionary<uint, SchemaAttribute> attributesByAttrtyp;
        private IDictionary<string, SchemaAttribute> attributesByName;
        private IDictionary<string, int> classesByName;

        public DirectorySchema(IsamDatabase database)
        {
            TableDefinition dataTable = database.Tables[ADConstants.DataTableName];
            this.LoadColumnList(dataTable.Columns);
            this.LoadAttributeIndices(dataTable.Indices2);
            using (var cursor = database.OpenCursor(ADConstants.DataTableName))
            {
                this.LoadClassList(cursor);
                this.LoadAttributeProperties(cursor);
                this.LoadPrefixMap(cursor);
            }
            // TODO: Load Ext-Int Map from hiddentable
        }

        /// <summary>
        /// Gets the OID prefix map.
        /// </summary>
        public PrefixMap PrefixMapper
        {
            get;
            private set;
        }

        public ICollection<SchemaAttribute> FindAllAttributes()
        {
            return this.attributesByName.Values;
        }

        public SchemaAttribute FindAttribute(string attributeName)
        {
            Validator.AssertNotNullOrWhiteSpace(attributeName, "attributeName");
            SchemaAttribute attribute;
            bool found = this.attributesByName.TryGetValue(attributeName.ToLower(), out attribute);
            if (found)
            {
                return attribute;
            }
            else
            {
                throw new SchemaAttributeNotFoundException(attributeName);
            }
        }

        public bool ContainsAttribute(string attributeName)
        {
            Validator.AssertNotNullOrWhiteSpace(attributeName, "attributeName");
            return this.attributesByName.ContainsKey(attributeName.ToLower());
        }

        public SchemaAttribute FindAttribute(uint attrtyp)
        {
            SchemaAttribute attribute;
            bool found = this.attributesByAttrtyp.TryGetValue(attrtyp, out attribute);
            if(found)
            {
                return attribute;
            }
            else
            {
                throw new SchemaAttributeNotFoundException(attrtyp);
            }
        }

        public Columnid FindColumnId(string attributeName)
        {
            return this.FindAttribute(attributeName).ColumnID;
        }

        public string FindIndexName(string attributeName)
        {
            return this.FindAttribute(attributeName).Index;
        }

        public int FindClassId(string className)
        {
            if(this.classesByName.ContainsKey(className))
            {
                return this.classesByName[className];
            }
            else
            {
                // TODO: Move to resources.
                string message = String.Format("Class {0} has not been found in the schema.", className);
                throw new DirectoryObjectNotFoundException(message);
            }
        }

        private void LoadColumnList(ColumnCollection columns)
        {
            this.attributesByName = new Dictionary<string, SchemaAttribute>(columns.Count);
            this.attributesByAttrtyp = new Dictionary<uint, SchemaAttribute>(columns.Count);

            foreach (var column in columns)
            {
                var attr = new SchemaAttribute();
                attr.ColumnName = column.Name;
                attr.ColumnID = column.Columnid;

                if (IsAttributeColumn(attr.ColumnName))
                {
                    // Column is mapped to LDAP attribute
                    attr.InternalId = GetAttrtypFromColumnName(attr.ColumnName);
                    attributesByAttrtyp.Add(attr.InternalId.Value, attr);
                }
                else
                {
                    // System column. These normally do not appear in schema.
                    attr.IsSystemOnly = true;
                    attr.SystemFlags = AttributeSystemFlags.NotReplicated | AttributeSystemFlags.Base | AttributeSystemFlags.DisallowRename | AttributeSystemFlags.Operational;
                    // Approximate Syntax from ColumnId
                    attr.Syntax = GetSyntaxFromColumnType(column.Columnid);
                    attr.OmSyntax = AttributeOmSyntax.Undefined;
                    attr.Name = NormalizeSystemColumnName(attr.ColumnName);
                    this.attributesByName.Add(attr.Name.ToLower(), attr);
                }
            }
        }

        private void LoadAttributeIndices(IEnumerable<IndexInfo> indices)
        {
            //HACK: We are using low-level IndexInfo instead of high-level IndexCollection.
            /* There is a bug in Isam IndexCollection enumerator, which causes it to loop indefinitely
             * through the first few indices under some very rare circumstances. */
            foreach (var index in indices)
            {
                var segments = index.IndexSegments;
                if (segments.Count == 1)
                {
                    // We support only simple indexes
                    SchemaAttribute attr = FindAttributeByIndexName(index.Name);
                    if (attr != null)
                    {
                        // We found a single attribute to which this index corresponds
                        attr.Index = index.Name;
                    }
                }
            }

            // Manually assign PDNT_index to PDNT_col
            var pdnt = FindAttribute(CommonDirectoryAttributes.ParentDNTag);
            pdnt.Index = ParentDNTagIndex;
        }

        private void LoadAttributeProperties(Cursor dataTableCursor)
        {
            // With these built-in attributes, ID == Internal ID
            Columnid attributeIdCol = this.attributesByAttrtyp[CommonDirectoryAttributes.AttributeIdAttrtyp].ColumnID;
            SchemaAttribute ldapDisplayNameAtt = this.attributesByAttrtyp[CommonDirectoryAttributes.LdapDisplayNameAttrtyp];
            Columnid ldapDisplayNameCol = ldapDisplayNameAtt.ColumnID;
            // Set index to ldapDisplayName so that we can find attributes by their name
            dataTableCursor.CurrentIndex = ldapDisplayNameAtt.Index;

            // Load attribute ids of attributeSchema attributes by doing DB lookups
            // TODO: Hardcode IDs of these attributes so that we do not have to do DB lookups?
            Columnid internalIdCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.InternalId);
            Columnid linkIdCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.LinkId);
            Columnid isSingleValuedCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.IsSingleValued);
            Columnid attributeSyntaxCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.AttributeSyntax);
            Columnid isInGlobalCatalogCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.IsInGlobalCatalog);
            Columnid searchFlagsCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SearchFlags);
            Columnid systemOnlyCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SystemOnly);
            Columnid syntaxCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.AttributeSyntax);
            Columnid omSyntaxCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.AttributeOmSyntax);
            Columnid cnCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.CommonName);
            Columnid rangeLowerCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.RangeLower);
            Columnid rangeUpperCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.RangeUpper);
            Columnid schemaGuidCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SchemaGuid);
            Columnid systemFlagsCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SystemFlags);
            Columnid isDefunctCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.IsDefunct);

            // Now traverse through all schema attributes and load their properties
            // Use this filter: (objectCategory=attributeSchema)
            dataTableCursor.CurrentIndex = this.attributesByAttrtyp[CommonDirectoryAttributes.ObjectCategoryId].Index;
            dataTableCursor.FindRecords(MatchCriteria.EqualTo, Key.Compose(CommonDirectoryCl));
            while (dataTableCursor.MoveNext())
            {
                uint? internalId = dataTableCursor.RetrieveColumnAsUInt(internalIdCol);
                uint attributeId = dataTableCursor.RetrieveColumnAsUInt(attributeIdCol).Value;
                // Some built-in attributes do not have internal id set, which means it is equal to the public id
                uint attrtyp = internalId ?? attributeId;
                SchemaAttribute attribute;
                bool found = this.attributesByAttrtyp.TryGetValue(attrtyp, out attribute);
                if (! found)
                {
                    // We are loading info about a new attribute
                    attribute = new SchemaAttribute();
                    attribute.InternalId = internalId;
                }
                attribute.Id = dataTableCursor.RetrieveColumnAsUInt(attributeIdCol).Value;
                attribute.Name = dataTableCursor.RetrieveColumnAsString(ldapDisplayNameCol);
                attribute.CommonName = dataTableCursor.RetrieveColumnAsString(cnCol);
                attribute.RangeLower = dataTableCursor.RetrieveColumnAsInt(rangeLowerCol);
                attribute.RangeUpper = dataTableCursor.RetrieveColumnAsInt(rangeUpperCol);
                attribute.SchemaGuid = dataTableCursor.RetrieveColumnAsGuid(schemaGuidCol).Value;
                attribute.IsDefunct = dataTableCursor.RetrieveColumnAsBoolean(isDefunctCol);
                attribute.SystemFlags = dataTableCursor.RetrieveColumnAsAttributeSystemFlags(systemFlagsCol);
                attribute.LinkId = dataTableCursor.RetrieveColumnAsInt(linkIdCol);
                attribute.IsInGlobalCatalog = dataTableCursor.RetrieveColumnAsBoolean(isInGlobalCatalogCol);
                attribute.IsSingleValued = dataTableCursor.RetrieveColumnAsBoolean(isSingleValuedCol);
                attribute.SearchFlags = dataTableCursor.RetrieveColumnAsSearchFlags(searchFlagsCol);
                attribute.IsSystemOnly = dataTableCursor.RetrieveColumnAsBoolean(systemOnlyCol);
                attribute.Syntax = dataTableCursor.RetrieveColumnAsAttributeSyntax(syntaxCol);
                attribute.OmSyntax = dataTableCursor.RetrieveColumnAsAttributeOmSyntax(omSyntaxCol);
                // Make it case-insensitive by always lowering the name:
                this.attributesByName.Add(attribute.Name.ToLower(), attribute);
            }
        }

        private Columnid LoadColumnIdByAttributeName(Cursor cursor, string attributeName)
        {
            Columnid attributeIdCol = this.attributesByAttrtyp[CommonDirectoryAttributes.AttributeIdAttrtyp].ColumnID;
            // Assume that attributeNameIndex is set as the current index
            cursor.GotoKey(Key.Compose(attributeName));
            uint attributeId = cursor.RetrieveColumnAsUInt(attributeIdCol).Value;
            // return this.attributesByAttrtyp[attributeId].ColumnID;
        }

        private void LoadClassList(Cursor dataTableCursor)
        {
            // Initialize the class list
            this.classesByName = new Dictionary<string, int>();

            // Preload column IDs to spare some time in the loop.
            Columnid dntCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.DNTag);
            Columnid governsIdCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.GovernsId);
            Columnid cnCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.CommonName);
            Columnid schemaGuidCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SchemaGuid);
            Columnid classCategoryCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.ObjectClassCategory);
            Columnid isDefunctCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.IsDefunct);
            Columnid systemOnlyCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SystemOnly);
            Columnid systemFlagsCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.SystemFlags);
            Columnid internalIdCol = this.LoadColumnIdByAttributeName(dataTableCursor, CommonDirectoryAttributes.InternalId);
            SchemaAttribute ldapDisplayNameAtt = this.FindAttribute(CommonDirectoryAttributes.LdapDisplayNameAttrtyp);

            // Search for all classes using this heuristics: (&(ldapDisplayName=*)(governsId=*))
            dataTableCursor.CurrentIndex = ldapDisplayNameAtt.Index;
            while (dataTableCursor.MoveNext())
            {
                uint? governsId = dataTableCursor.RetrieveColumnAsUInt(governsIdCol);
                if(!governsId.HasValue)
                {
                    // This is an attribute and not a class, so we skip to the next object.
                    continue;
                }

                int dnt = dataTableCursor.RetrieveColumnAsDNTag(dntCol).Value;
                string name = dataTableCursor.RetrieveColumnAsString(ldapDisplayNameAtt.ColumnID);
                string commonName = dataTableCursor.RetrieveColumnAsString(cnCol);

                var schemaClass = new SchemaClass(name, commonName, dnt, governsId.Value);
                schemaClass.IsDefunct = dataTableCursor.RetrieveColumnAsBoolean(isDefunctCol);
                schemaClass.SchemaGuid = dataTableCursor.RetrieveColumnAsGuid(schemaGuidCol).Value;
                schemaClass.IsDefunct = dataTableCursor.RetrieveColumnAsBoolean(isDefunctCol);
                // schemaClass.SystemFlags = dataTableCursor.RetrieveColumnAsAttributeSystemFlags(systemFlagsCol);
                schemaClass.IsSystemOnly = dataTableCursor.RetrieveColumnAsBoolean(systemOnlyCol);
                schemaClass.ClassType = dataTableCursor.RetrieveColumnAsClassType(classCategoryCol);
                schemaClass.InternalId = dataTableCursor.RetrieveColumnAsUInt(internalIdCol);
                // Oid
                classesByName.Add(name, dnt);
            }
        }

        private void LoadPrefixMap(Cursor dataTableCursor)
        {
            // Find the Schema Naming Context using this filter: (objectCategory=dMD)
            dataTableCursor.FindAllRecords();
            dataTableCursor.CurrentIndex = this.FindIndexName(CommonDirectoryAttributes.ObjectCategory);
            int schemaObjectCategoryId = this.FindClassId(CommonDirectoryClasses.Schema);
            bool schemaFound = dataTableCursor.GotoKey(Key.Compose(schemaObjectCategoryId));

            // Load the prefix map from this object
            var prefixMapColId = this.FindColumnId(CommonDirectoryAttributes.PrefixMap);
            byte[] binaryPrefixMap = dataTableCursor.RetrieveColumnAsByteArray(prefixMapColId);
            this.PrefixMapper = new PrefixMap(binaryPrefixMap);

            foreach(var attribute in this.attributesByName.Values)
            {
                if (attribute.Id.HasValue)
                {
                    attribute.Oid = this.PrefixMapper.Translate((uint)attribute.Id.Value);
                }
            }
        }

        private SchemaAttribute FindAttributeByIndexName(string indexName)
        {
            SchemaAttribute attribute = null;
            if(IsAttributeColumnIndex(indexName))
            {
                uint attrtyp = GetAttrtypFromIndexName(indexName);
                this.attributesByAttrtyp.TryGetValue(attrtyp, out attribute);
            }
            else
            {
                string systemColName = NormalizeIndexName(indexName);
                this.attributesByName.TryGetValue(systemColName.ToLower(), out attribute);
            }
            return attribute;
        }

        private static AttributeSyntax GetSyntaxFromColumnType(Columnid column)
        {
            Type colType = column.Type;

            if(colType == typeof(int))
            {
                return AttributeSyntax.Int;
            }
            else if(colType == typeof(Int64))
            {
                return AttributeSyntax.Int64;
            }
            else if (colType == typeof(byte))
            {
                return AttributeSyntax.Bool;
            }
            else if (colType == typeof(byte[]))
            {
                return AttributeSyntax.OctetString;
            }
            else
            {
                return AttributeSyntax.Undefined;
            }
        }

        private static bool IsAttributeColumn(string columnName)
        {
            // Attributes are stored in columns starting with ATTx
            return columnName.StartsWith(AttributeColPrefix);
        }

        private static bool IsAttributeColumnIndex(string indexName)
        {
            return indexName.StartsWith(AttributeColIndexPrefix);
        }
        
        private static uint GetAttrtypFromColumnName(string columnName)
        {
            // Strip the ATTx prefix from column name to get the numeric attribute ID
            string attrtypStr = columnName.Substring(AttributeColPrefix.Length + 1, columnName.Length - AttributeColPrefix.Length - 1);
            
            // Parse the rest as int. May be negative (and 3rd party attributes are).
            return UInt32.Parse(attrtypStr);
        }

        /// <remarks>
        /// Columns that correspond to AD attributes have names in the form ATTXYYYYYYYY
        /// where X is an encoding of the attribute syntax and YYYYYYYY is the ATTRTYP of the attribute.
        /// </remarks>
        private static string GetColumnNameFromAttrtyp(int attrtyp, AttributeSyntax syntax)
        {
            var result = new StringBuilder(AttributeColPrefix);
            char encodedSyntax = (char)(syntax + 'a');
            // Example: Oid -> c, Sid -> r
            // Exception: msDS-DevicePhysicalIDs is stored in column ATTk59075 but is of type UnicodeString.
            result.Append(encodedSyntax);
            result.Append(attrtyp);
            return result.ToString();
        }

        private static uint GetAttrtypFromIndexName(string indexName)
        {
            // Strip the INDEX_ prefix from index name to get the numeric attribute ID
            string attrtypStr = NormalizeIndexName(indexName);
            byte[] binaryAttrtyp = attrtypStr.HexToBinary();

            if (BitConverter.IsLittleEndian)
            {
                // Reverse byte order
                binaryAttrtyp.SwapBytes(0, 3);
                binaryAttrtyp.SwapBytes(1, 2);
            }

            return BitConverter.ToUInt32(binaryAttrtyp, 0);
        }

        private static string NormalizeSystemColumnName(string columnName)
        {
            if (columnName.EndsWith(SystemColSuffix))
            {
                // Strip the _col suffix (e.g. PDNT_col)
                return columnName.Substring(0, columnName.Length - SystemColSuffix.Length);
            }
            else
            {
                // Don't do any change
                return columnName;
            }
        }

        private static string NormalizeIndexName(string indexName)
        {
            // Strip any suffix or prefix from index
            if(indexName.StartsWith(AttributeColIndexPrefix))
            {
                // The index name can start with INDEX_ or INDEX_T_ (e.g. INDEX_T_9EBE46C2), so we get the last component
                indexName = indexName.Split(IndexNameComponentSeparator).Last();
            }
            if(indexName.EndsWith(SystemColIndexSuffix))
            {
                indexName = indexName.Substring(0, indexName.Length - SystemColIndexSuffix.Length);
            }
            return indexName;   
        }
    }
}
