using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ModelMapperGenerator.Descriptors
{
    internal readonly struct PropertySymbolDescriptor(IPropertySymbol symbol)
    {
        private readonly List<string> _returnTypeRelatedNamespaces = [];

        public readonly string ReturnTypeNamespace { get; } = symbol.Type.ContainingNamespace.Name;
        public readonly string ReturnTypeName { get; } = symbol.Type.Name;
        public readonly string ReturnTypeFullyQualifiedName { get; } = symbol.Type.ToDisplayString();
        public readonly string PropertyName { get; } = symbol.Name;
        public readonly bool HasPublicGetter { get; } = symbol.GetMethod is not null && symbol.GetMethod.DeclaredAccessibility == Accessibility.Public;
        public readonly bool HasPublicSetter { get; } = symbol.SetMethod is not null && symbol.SetMethod.DeclaredAccessibility == Accessibility.Public;

        public static void CreateAndInsert(PropertySymbolDescriptor[] array, IPropertySymbol property, ref int currentIndex)
        {
            array[currentIndex] = new PropertySymbolDescriptor(property);
            currentIndex++;
        }

        public readonly void MarkReturnTypeAsRelatedTypeInNamespace(string namespaceName) => _returnTypeRelatedNamespaces.Add(namespaceName);

        public readonly bool ReturnTypeIsRelatedTypeInNamespace(string namespaceName)
        {
            foreach (var knownNamespace in _returnTypeRelatedNamespaces)
            {
                if (knownNamespace == namespaceName)
                {
                    return true;
                }
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return (HasPublicGetter, HasPublicSetter, PropertyName, ReturnTypeName, ReturnTypeNamespace).GetHashCode();
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PropertySymbolDescriptor other && other == this;
        }

        public static bool operator ==(PropertySymbolDescriptor left, PropertySymbolDescriptor right)
        {
            return left.HasPublicGetter == right.HasPublicGetter &&
                   left.HasPublicSetter == right.HasPublicSetter &&
                   left.PropertyName == right.PropertyName &&
                   left.ReturnTypeName == right.ReturnTypeName &&
                   left.ReturnTypeNamespace == right.ReturnTypeNamespace;
        }

        public static bool operator !=(PropertySymbolDescriptor left, PropertySymbolDescriptor right)
        {
            return left.HasPublicGetter != right.HasPublicGetter ||
                   left.HasPublicSetter != right.HasPublicSetter ||
                   left.PropertyName != right.PropertyName ||
                   left.ReturnTypeName != right.ReturnTypeName ||
                   left.ReturnTypeNamespace != right.ReturnTypeNamespace;
        }
    }
}
