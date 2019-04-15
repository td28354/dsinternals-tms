namespace DSInternals.Common.Exceptions
{
    using DSInternals.Common.Properties;
    using System;

    [Serializable]
    public sealed class SchemaAttributeNotFoundException : DirectoryException
    {
        public object AttributeIdentifier
        {
            get;
            private set;
        }
        public SchemaAttributeNotFoundException(string attributeName) : base(null)
        {
            this.AttributeIdentifier = attributeName;
        }
        public SchemaAttributeNotFoundException(uint attrtyp)
            : base(null)
        {
            this.AttributeIdentifier = attrtyp;
        }
        public override string Message
        {
            get
            {
                return string.Format(Resources.AttributeNotFoundMessageFormat, this.AttributeIdentifier);
            }
        }
    }
}
