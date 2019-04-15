namespace DSInternals.DataStore
{
    using System;
    using DSInternals.Common;

    public abstract class SchemaObject
    {
        public SchemaObject(string ldapDisplayName, string commonName, int dnTag)
        {
            Validator.AssertNotNullOrWhiteSpace(ldapDisplayName, nameof(ldapDisplayName));
            Validator.AssertNotNullOrWhiteSpace(commonName, nameof(commonName));

            this.Name = ldapDisplayName;
            this.CommonName = commonName;
            this.DNTag = dnTag;
        }

        /// <summary>
        /// Gets the ldapDisplayName of the class/attribute object.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the Common Name (CN) of the class/attribute object.
        /// </summary>
        public string CommonName
        {
            get;
            private set;
        }

        /// <summary>
        /// Distinguished name tag of the class/attribute object
        /// </summary>
        public int DNTag
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the OID for the class/attribute object.
        /// </summary>
        public string Oid
        {
            get;
            internal set;
        }

        /// <summary>
        /// This optional attribute can be used to uniquely identify the associated class/attribute object.
        /// </summary>
        public uint? InternalId
        {
            get;
            internal set;
        }

        /// <summary>
        /// ATTRTYP is a compact representation of an OID.
        /// </summary>
        /// <remarks>
        /// The ATTRTYP space is 32 bits wide and is divided into the following ranges.
        /// [0x00000000..0x7FFFFFFF] - ATTRTYPs that map to OIDs via the prefix table.
        /// [0x80000000..0xBFFFFFFF] - ATTRTYPs used as values of msDS-IntId attribute.
        /// [0xC0000000..0xFFFEFFFF] - Reserved for future use.
        /// [0xFFFF0000..0xFFFFFFFF] - Reserved for internal use(never appear on the wire).
        /// </remarks>
        /// <see>https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-adts/98b55783-7029-4a04-8f8b-9df9344089c3</see>
        public uint? Attrtyp
        {
            get
            {
                // MS-DRSR AttrtypFromSchemaObj procedure:
                return this.InternalId.HasValue ? this.InternalId : this.SchemaId;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the class/attribute object is defunct.
        /// </summary>
        public bool IsDefunct
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether only the system can modify this class/attribute.
        /// </summary>
        public bool IsSystemOnly
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the schemaIDGuid for the class/attribute object.
        /// </summary>
        public Guid SchemaGuid
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the identifier of the schema object. It is mapped to governsId for classes and to attributeId for attributes.
        /// </summary>
        protected abstract uint? SchemaId
        {
            get;
        }
    }
}
