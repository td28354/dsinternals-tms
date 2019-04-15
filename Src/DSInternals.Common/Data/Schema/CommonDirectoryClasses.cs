namespace DSInternals.Common.Data
{
    public static class CommonDirectoryClasses
    {
        /// <summary>
        /// A Local Security Authority secret that is used for trust relationships and to save service passwords.
        /// </summary>
        public const string Secret = "secret";

        /// <summary>
        /// Stores information about an employee or contractor who works for an organization.
        /// </summary>
        public const string User = "user";

        /// <summary>
        /// Defines a class object in the schema.
        /// </summary>
        public const string ClassSchema = "classSchema";
        public const int ClassSchemaAttrtyp = 196621;

        /// <summary>
        /// Defines an attribute object in the schema.
        /// </summary>
        public const string AttributeSchema = "attributeSchema";
        public const int AttributeSchemaAttrtyp = 196622;

        /// <summary>
        /// Holds the schema for Active Directory Domain Services (AD DS) and the Active Directory directory service. The Lightweight Directory Access Protocol (LDAP) name dMD stands for Directory Management Domain.
        /// </summary>
        public const string Schema = "dMD";

        /// <summary>
        /// Root keys for the Group Key Distribution Service.
        /// </summary>
        public const string KdsRootKey = "msKds-ProvRootKey";

        /// <summary>
        /// Represents the Active Directory DSA process on the server.
        /// </summary>
        public const string WritableDSA = "nTDSDSA";

        /// <summary>
        /// A subclass of Directory Service Agent that is distinguished by its reduced privilege level.
        /// </summary>
        public const string ReadOnlyDSA = "nTDSDSARO";
    }
}
