using System;

namespace ModelMapperGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModelGenerationTarget : Attribute
    {
        public Type[] FullyQualifiedTypes { get; set; }
    }
}
