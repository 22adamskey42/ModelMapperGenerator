using System.Collections.Generic;

namespace ModelMapperGenerator.Analyzers.Descriptors
{
    internal sealed class TypeNameDescriptorComparer : IEqualityComparer<TypeNameDescriptor>
    {
        public bool Equals(TypeNameDescriptor x, TypeNameDescriptor y)
        {
            return x == y;
        }

        public int GetHashCode(TypeNameDescriptor obj)
        {
            return obj.GetHashCode();
        }
    }
}
