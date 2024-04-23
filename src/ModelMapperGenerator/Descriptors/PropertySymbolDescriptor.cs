using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ModelMapperGenerator.Descriptors
{
    internal readonly struct PropertySymbolDescriptor
    {
        private readonly List<string> _returnTypeRelatedNamespaces = [];

        public readonly bool IsNullable { get; }
        public readonly TypeKind ReturnTypeKind { get; }
        public readonly string ReturnTypeNamespace { get; }
        public readonly string ReturnTypeName { get; }
        public readonly string ReturnTypeFullyQualifiedName { get; }
        public readonly string PropertyName { get; }
        public readonly bool HasPublicGetter { get; }
        public readonly bool HasPublicSetter { get; }

        private PropertySymbolDescriptor(IPropertySymbol propertySymbol)
        {
            PropertyName = propertySymbol.Name;
            HasPublicGetter = propertySymbol.GetMethod is not null && propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public;
            HasPublicSetter = propertySymbol.SetMethod is not null && propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public;
            IsNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
            ITypeSymbol actualType = IsNullable
                ? ((INamedTypeSymbol)propertySymbol.Type).TypeArguments[0]
                : (INamedTypeSymbol)propertySymbol.Type;
            ReturnTypeNamespace = actualType.ContainingNamespace.Name;
            ReturnTypeName = actualType.Name;
            ReturnTypeFullyQualifiedName = actualType.ToDisplayString();
            ReturnTypeKind = actualType.TypeKind;
        }

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
