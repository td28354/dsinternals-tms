namespace DSInternals.Replication.Model
{
    using DSInternals.Common;
    using DSInternals.Common.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;

    public class ReplicaObject : DirectoryObject
    {
        private string distinguishedName;
        private Guid guid;
        private SecurityIdentifier sid;

        public ReplicaObject(String distinguishedName, Guid objectGuid, SecurityIdentifier objectSid, ReplicaAttributeCollection attributes)
        {
            this.guid = objectGuid;
            this.distinguishedName = distinguishedName;
            this.sid = objectSid;
            this.Attributes = attributes;
        }
        // TODO: ISchema
        public BasicSchema Schema
        {
            get;
            set;
        }

        public override string DistinguishedName
        {
            get
            {
                return this.distinguishedName;
            }
        }

        public override Guid Guid
        {
            get
            {
                return this.guid;
            }
        }

        public override SecurityIdentifier Sid
        {
            get
            {
                return this.sid;
            }
        }

        // TODO: Read only collection
        public ReplicaAttributeCollection Attributes
        {
            get;
            private set;
        }

        public void LoadLinkedValues(ReplicatedLinkedValueCollection linkedValueCollection)
        {
            var objectAttributes = linkedValueCollection.Get(this.Guid);

            // Only continue if the linked values contain attributes of this AD object
            if(objectAttributes != null)
            {
                foreach (var attribute in objectAttributes)
                {
                    this.Attributes.Add(attribute);
                }
            }
        }

        protected bool HasAttribute(int attributeId)
        {
            return this.Attributes.ContainsKey(attributeId);
        }

        protected void ReadAttribute(int attributeId, out byte[][] values)
        {
            values = null;
            ReplicaAttribute attribute;
            bool hasAttribute = this.Attributes.TryGetValue(attributeId, out attribute);
            if (hasAttribute)
            {
                bool hasValue = attribute.Values != null && attribute.Values.Length > 0;
                if (hasValue)
                {
                    values = attribute.Values;
                }
            }
        }

        public void ReadAttribute(int attributeId, out byte[] value)
        {
            this.ReadAttribute(attributeId, out value, 0);
        }

        public void ReadAttribute(int attributeId, out byte[] value, int valueIndex)
        {
            byte[][] values;
            this.ReadAttribute(attributeId, out values);
            bool containsValue = values != null && values.Length > valueIndex;
            value = containsValue ? values[valueIndex] : null;
        }

        public void ReadAttribute(int attributeId, out int? value)
        {
            byte[] binaryValue;
            this.ReadAttribute(attributeId, out binaryValue);
            value = (binaryValue != null) ? BitConverter.ToInt32(binaryValue, 0) : (int?)null;
        }

        public void ReadAttribute(int attributeId, out long? value)
        {
            byte[] binaryValue;
            this.ReadAttribute(attributeId, out binaryValue);
            value = (binaryValue != null) ? BitConverter.ToInt64(binaryValue, 0) : (long?)null;
        }

        public void ReadAttribute(int attributeId, out string value)
        {
            byte[] binaryValue;
            this.ReadAttribute(attributeId, out binaryValue);
            value = (binaryValue != null) ? Encoding.Unicode.GetString(binaryValue) : null;
        }

        public void ReadAttribute(int attributeId, out string[] values)
        {
            values = null;
            byte[][] binaryValues;
            this.ReadAttribute(attributeId, out binaryValues);
            if(binaryValues != null)
            {
                values = binaryValues.Select(item => Encoding.Unicode.GetString(item)).ToArray();
            }
        }

        public void ReadAttribute(int attributeId, out DistinguishedName value)
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        public void ReadAttribute(int attributeId, out SecurityIdentifier value)
        {
            byte[] binaryValue;
            this.ReadAttribute(attributeId, out binaryValue);
            value = (binaryValue != null) ? new SecurityIdentifier(binaryValue, 0) : null;
        }
        public void ReadAttribute(int attributeId, out SamAccountType? value)
        {
            int? numericValue;
            this.ReadAttribute(attributeId, out numericValue);
            value = numericValue.HasValue ? (SamAccountType)numericValue.Value : (SamAccountType?)null;
        }
        public void ReadAttribute(int attributeId, out bool value)
        {
            int? numericValue;
            this.ReadAttribute(attributeId, out numericValue);
            value = numericValue.HasValue ? numericValue.Value != 0 : false;
        }

        public override bool HasAttribute(string name)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            return this.HasAttribute(attributeId);
        }

        public override void ReadAttribute(string name, out byte[] value)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            this.ReadAttribute(attributeId, out value);
        }

        public override void ReadAttribute(string name, out byte[][] value)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            this.ReadAttribute(attributeId, out value);
        }

        public override void ReadAttribute(string name, out int? value)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            this.ReadAttribute(attributeId, out value);
        }

        public override void ReadAttribute(string name, out long? value)
        {
            //values = 0;
            int attributeId = this.Schema.FindAttributeId(name);

            //if (name.Equals("pwdLastSet2", StringComparison.OrdinalIgnoreCase))
            //{
            //    long? valueTemp = null;
            //    for (int i = 1; i < 100000000; i++)
            //    {
            //        try
            //        {
            //            this.ReadAttribute(i, out valueTemp);
            //        }
            //        catch (Exception ex)
            //        {
            //            continue;
            //        }

            //        if (valueTemp.HasValue && valueTemp == 24)
            //        {
            //            //values = valueTemp;
            //            values = i;
            //            return;
            //        }
            //    }
            //    values = null;
            //}
            //else
            //{
            //    this.ReadAttribute(attributeId, out values);
            //}

            this.ReadAttribute(attributeId, out value);
        }

        public List<string> ReadAllAttributes()
        {
            var values = new List<string>();

            values.AddRange(this.ReadAllIntegers());
            values.AddRange(this.ReadAllLongs());
            values.AddRange(this.ReadAllStrings());
            values.AddRange(this.ReadAllStringArrays());
            values.AddRange(this.ReadAllDistinguishedNames());

            return values;
        }

        public List<string> ReadAllIntegers()
        {
            var values = new List<string>();

            int? valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (valueTemp.HasValue)
                {
                    //values = valueTemp;
                    values.Add($"Type = integer; Index = {i}, Value = {valueTemp.Value}");
                }
            }

            return values;
        }

        public List<string> ReadAllLongs()
        {
            var values = new List<string>();

            long? valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (valueTemp.HasValue)
                {
                    //values = valueTemp;
                    values.Add($"Type = long; Index = {i}, Value = {valueTemp.Value}");
                }
            }

            return values;
        }

        public List<string> ReadAllLongsExtra()
        {
            const double OneHundredNanosecond = .000000100;
            const int SecondsInDay = 86400;

            var values = new List<string>();

            long? valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (valueTemp.HasValue)
                {
                    //values = valueTemp;
                    var maxPasswordAge = Math.Abs((long)((long)valueTemp.Value * OneHundredNanosecond) / SecondsInDay);
                    values.Add($"Type = longExtra; Index = {i}, Value = {maxPasswordAge}");
                }
            }

            return values;
        }

        public List<string> ReadAllStrings()
        {
            var values = new List<string>();

            string valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(valueTemp))
                {
                    //values = valueTemp;
                    values.Add($"Type = string; Index = {i}, Value = {valueTemp}");
                }
            }

            return values;
        }

        public List<string> ReadAllBools()
        {
            var values = new List<string>();

            bool valueTemp = false;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }

                //values = valueTemp;
                values.Add($"Type = bool; Index = {i}, Value = {valueTemp}");
            }

            return values;
        }

        public List<string> ReadAllStringArrays()
        {
            var values = new List<string>();
            string[] valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }
                if (valueTemp != null && valueTemp.Length > 0)
                {
                    //values = valueTemp;
                    values.Add($"Type = string[]; Index = {i}, Value = {string.Join(",", valueTemp)}");
                }
            }
            return values;
        }

        public List<string> ReadAllDistinguishedNames()
        {
            var values = new List<string>();
            DistinguishedName valueTemp = null;
            for (int i = 1; i < 100000000; i++)
            {
                try
                {
                    this.ReadAttribute(i, out valueTemp);
                }
                catch (Exception ex)
                {
                    continue;
                }
                if (valueTemp != null)
                {
                    //values = valueTemp;
                    values.Add($"Type = DistinguishedName; Index = {i}, Value = {valueTemp.ToString()}");
                }
            }
            return values;
        }

        public override void ReadAttribute(string name, out string value)
        {
            //values = string.Empty;
            int attributeId = this.Schema.FindAttributeId(name);
            //if (name.Equals("email", StringComparison.OrdinalIgnoreCase))
            //{
            //    var email = string.Empty;
            //    string valueTemp = string.Empty;

            //    for (int i = 1; i < 100000000; i++)
            //    {
            //        this.ReadAttribute(i, out valueTemp);
            //        if (string.IsNullOrWhiteSpace(valueTemp))
            //        {
            //            continue;
            //        }
            //        email += $"; i = {i} values = {valueTemp}";
            //    }

            //    values = email;
            //}
            //else
            //{
            //    this.ReadAttribute(attributeId, out values);
            //}

            this.ReadAttribute(attributeId, out value);
        }

        public override void ReadAttribute(string name, out string[] values)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            //var result = new List<string>();
            //if (name.Equals("email", StringComparison.OrdinalIgnoreCase))
            //{
            //    for (int i = 15; i < 100000000; i++)
            //    {
            //        this.ReadAttribute(i, out string[] valueTemp);
            //        if (valueTemp == null || valueTemp.Length == 0)
            //        {
            //            continue;
            //        }
            //        result.Add($"i = {i} values = {string.Join(",", valueTemp)}");
            //    }

            //    values = result.ToArray();
            //}
            //else
            //{
            //    this.ReadAttribute(attributeId, out values);
            //}

            this.ReadAttribute(attributeId, out values);
        }

        public override void ReadAttribute(string name, out DistinguishedName value)
        {
            int attributeId = this.Schema.FindAttributeId(name);
            this.ReadAttribute(attributeId, out value);
        }

        public override void ReadLinkedValues(string attributeName, out byte[][] values)
        {
            // The linked values have already been merged with regular attributes using LoadLinkedValues
            // TODO: We currently only support DN-Binary linked values.
            // TODO: Check if the attribute exists
            byte[][] rawValues;
            this.ReadAttribute(attributeName, out rawValues);
            values = (rawValues != null) ? rawValues.Select(rawValue => ParseDNBinary(rawValue)).ToArray() : null;
        }

        protected override bool HasBigEndianRid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the binary data as SYNTAX_DISTNAME_BINARY.
        /// </summary>
        /// <param name="blob">SYNTAX_DISTNAME_BINARY structure</param>
        /// <returns>Binary values</returns>
        /// <see>https://msdn.microsoft.com/en-us/library/cc228431.aspx</see>
        protected static byte[] ParseDNBinary(byte[] blob)
        {
            // Read structLen (4 bytes): The length of the structure, in bytes, up to and including the field StringName.
            int structLen = BitConverter.ToInt32(blob, 0);

            // Skip Padding (variable): The padding (bytes with values zero) to align the field dataLen at a double word boundary.
            int structLengthWithPadding = structLen;
            while(structLengthWithPadding % sizeof(int) != 0)
            {
                structLengthWithPadding++;
            }

            // Read dataLen (4 bytes): The length of the remaining structure, including this field, in bytes.
            int dataLen = BitConverter.ToInt32(blob, structLengthWithPadding);
            int dataOffset = structLengthWithPadding + sizeof(int);

            // Read byteVal(variable): An array of bytes.
            byte[] value = blob.Cut(dataOffset);

            // TODO: Validate the data length
            // Return only the binary data, without the DN.
            return value;
        }
    }
}
