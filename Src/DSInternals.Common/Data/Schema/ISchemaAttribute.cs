namespace DSInternals.Common.Data
{
    using System;

    public interface ISchemaAttribute
    {
        uint? Id { get; }
        string Name { get; }
        AttributeSyntax Syntax { get; }
    }
}
