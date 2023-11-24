using System.Collections.Generic;

namespace ModelMapperGenerator
{
    internal sealed class SymbolDescriptorComparer : IEqualityComparer<SymbolDescriptor?>
    {
        public bool Equals(SymbolDescriptor? x, SymbolDescriptor? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null && y is not null)
            {
                return false;
            }

            if (x is not null && y is null)
            {
                return false;
            }

            bool sameNamespace = x.SymbolNamespace == y.SymbolNamespace;
            bool sameName = x.SymbolName == y.SymbolName;
            bool sameMembers = x.SymbolMembers.SetEquals(y.SymbolMembers);
            return sameNamespace && sameName && sameMembers;
        }

        public int GetHashCode(SymbolDescriptor? obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}
