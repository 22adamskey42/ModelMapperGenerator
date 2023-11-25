using System;

namespace ModelMapperGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModelGenerationTargetAttribute : Attribute
    {
        public Type[] Types { get; set; }
    }
}
