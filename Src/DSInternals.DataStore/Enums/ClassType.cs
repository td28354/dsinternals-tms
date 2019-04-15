namespace DSInternals.DataStore
{
    /// <summary>
    /// Specifies the type of a schema class object.
    /// </summary>
    public enum ClassType : int
    {
        /// <summary>
        /// The class is a type 88 class.
        /// </summary>
        /// <remarks>
        /// Classes defined before 1993 are not required to be included in another category;
        /// assigning classes to categories was not required in the X.500 1988 specification.
        /// Classes defined prior to the X.500 1993 standards default to the 1988 class.
        /// This type of class is specified by a value of 0 in the objectClassCategory attribute.
        /// </remarks>
        Type88 = 0,

        /// <summary>
        /// The class is a structural class.
        /// </summary>
        Structural = 1,

        /// <summary>
        /// The class is an abstract class.
        /// </summary>
        Abstract = 2,

        /// <summary>
        /// The class is an auxiliary class.
        /// </summary>
        Auxiliary = 3,
    }
}
