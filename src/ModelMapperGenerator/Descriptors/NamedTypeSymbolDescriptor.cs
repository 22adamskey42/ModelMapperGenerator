using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ModelMapperGenerator.Descriptors
{
    internal sealed class NamedTypeSymbolDescriptor : IEquatable<NamedTypeSymbolDescriptor>
    {
        public INamedTypeSymbol Symbol { get; private set; } = null!;
        public FieldSymbolDescriptor[]? EnumSymbolMembers { get; private set; }
        public PropertySymbolDescriptor[]? ClassSymbolMembers { get; private set; }
        public string SymbolName { get; private set; } = null!;
        public string SymbolNamespaceName { get; private set; } = null!;

        private NamedTypeSymbolDescriptor() { }

        public static NamedTypeSymbolDescriptor? Create(INamedTypeSymbol symbol)
        {
            ImmutableArray<ISymbol> members = symbol.GetMembers();
            FieldSymbolDescriptor[]? enumDescriptors = null;
            PropertySymbolDescriptor[]? classDescriptors = null;

            if (symbol.TypeKind == TypeKind.Enum)
            {
                if (members.Length == 1 && members[0] is IMethodSymbol method && method.MethodKind == MethodKind.Constructor)
                {
                    return null;
                }

                enumDescriptors = CreateMembersField<FieldSymbolDescriptor, IFieldSymbol>(in members);
            }
            else if (symbol.TypeKind == TypeKind.Class)
            {
                classDescriptors = CreateMembersField<PropertySymbolDescriptor, IPropertySymbol>(in members);
            }

            NamedTypeSymbolDescriptor descriptor = new()
            {
                ClassSymbolMembers = classDescriptors,
                EnumSymbolMembers = enumDescriptors,
                Symbol = symbol,
                SymbolName = symbol.Name,
                SymbolNamespaceName = symbol.ContainingNamespace.ToDisplayString()
            };

            return descriptor;
        }

        public void MarkRelatedProperties(TargetAttributeDescriptor targetDescriptor)
        {
            if (ClassSymbolMembers is null)
            {
                return;
            }

            Span<PropertySymbolDescriptor> span = ClassSymbolMembers.AsSpan();
            foreach (ref PropertySymbolDescriptor prop in span)
            {
                foreach (string relatedType in targetDescriptor.RelatedTypes)
                {
                    if (prop.ReturnTypeFullyQualifiedName == relatedType)
                    {
                        prop.MarkReturnTypeAsRelatedTypeInNamespace(targetDescriptor.TargetNamespace);
                        break;
                    }
                }
            }
        }

        private static TDescriptor[] CreateMembersField<TDescriptor, TSymbol>(in ImmutableArray<ISymbol> members)
        {
            ReadOnlySpan<ISymbol> membersAsSpan = members.AsSpan();
            int count = GetMembersCount<TSymbol>(in membersAsSpan);
            TDescriptor[] elements = new TDescriptor[count];
            int index = 0;
            foreach (var member in membersAsSpan)
            {
                if (member is TSymbol symbol)
                {
                    if (typeof(TDescriptor) == typeof(PropertySymbolDescriptor) && typeof(TSymbol) == typeof(IPropertySymbol))
                    {
                        PropertySymbolDescriptor[] descriptorArray = Unsafe.As<PropertySymbolDescriptor[]>(elements);
                        PropertySymbolDescriptor.CreateAndInsert(descriptorArray, (IPropertySymbol)symbol, ref index);
                    }
                    else
                    {
                        FieldSymbolDescriptor[] descriptorArray = Unsafe.As<FieldSymbolDescriptor[]>(elements);
                        FieldSymbolDescriptor.CreateAndInsert(descriptorArray, (IFieldSymbol)symbol, ref index);
                    }
                }
            }

            return elements;
        }

        public bool Equals(NamedTypeSymbolDescriptor other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            bool sameName = this.SymbolName == other.SymbolName;
            bool sameNamespace = this.SymbolNamespaceName == other.SymbolNamespaceName;
            if (!sameName || !sameNamespace)
            {
                return false;
            }

            bool sameMembers = this.Symbol?.TypeKind switch
            {
                TypeKind.Enum => this.EnumSymbolMembers.SequenceEqual(other.EnumSymbolMembers),
                TypeKind.Class => this.ClassSymbolMembers.SequenceEqual(other.ClassSymbolMembers),
                _ => throw new ArgumentOutOfRangeException("Not supported type kind")
            };

            return sameMembers;
        }

        private static int GetMembersCount<TSymbol>(in ReadOnlySpan<ISymbol> spanOfMembers)
        {
            int count = 0;
            foreach (var member in spanOfMembers)
            {
                if (member is TSymbol)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
