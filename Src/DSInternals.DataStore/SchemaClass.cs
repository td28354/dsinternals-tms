namespace DSInternals.DataStore
{
    using System;

    /// <summary>
    /// Represents a schema class definition that is contained in the schema partition.
    /// </summary>
    public class SchemaClass : SchemaObject
    {
        public SchemaClass(string name, string commonName, int dnTag, uint governsId) : base(name, commonName, dnTag)
        {
            this.GovernsId = governsId;
        }

        /// <summary>
        /// Specifies the unique object ID of the class.
        /// </summary>
        public uint GovernsId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the type for the class object.
        /// </summary>
        public ClassType ClassType
        {
            get;
            internal set;
        }

        public override string ToString()
        {
            return String.Format("Class: {0}, DNT: {1}", Name, DNTag);
        }

        protected override uint? SchemaId
        {
            get
            {
                return this.GovernsId;
            }
        }
    }
}
