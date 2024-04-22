using Microsoft.CodeAnalysis;

namespace ModelMapperGenerator.Descriptors
{
    internal readonly struct FieldSymbolDescriptor(IFieldSymbol fieldSymbol)
    {
        public readonly string Name { get; } = fieldSymbol.Name;
        public readonly object? ConstantValue { get; } = fieldSymbol.ConstantValue;

        public static void CreateAndInsert(FieldSymbolDescriptor[] descriptors, IFieldSymbol symbol, ref int currentIndex)
        {
            descriptors[currentIndex] = new FieldSymbolDescriptor(symbol);
            currentIndex++;
        }

        public override bool Equals(object? obj)
        {
            return obj is FieldSymbolDescriptor descriptor && descriptor == this;
        }

        public override int GetHashCode()
        {
            return (Name, ConstantValue).GetHashCode();
        }

        public static bool operator == (FieldSymbolDescriptor left, FieldSymbolDescriptor right)
        {
            bool? constantsEqual = left.ConstantValue?.Equals(right.ConstantValue);
            bool namesEqual = left.Name == right.Name;
            if (constantsEqual.HasValue)
            {
                return namesEqual && constantsEqual.Value;
            }

            return namesEqual;
        }

        public static bool operator !=(FieldSymbolDescriptor left, FieldSymbolDescriptor right)
        {
            bool? constantsEqual = left.ConstantValue?.Equals(right.ConstantValue);
            bool namesEqual = left.Name == right.Name;
            if (constantsEqual.HasValue)
            {
                return !namesEqual || !constantsEqual.Value;
            }

            return !namesEqual;
        }
    }
}
