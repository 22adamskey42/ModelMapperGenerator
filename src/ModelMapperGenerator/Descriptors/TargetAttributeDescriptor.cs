using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Constants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ModelMapperGenerator.Descriptors
{
    internal sealed class TargetAttributeDescriptor : IEquatable<TargetAttributeDescriptor>
    {
        private readonly List<string> _relatedTypes;
        private readonly List<NamedTypeSymbolDescriptor> _containedTypes;

        public IReadOnlyList<string> RelatedTypes => _relatedTypes;

        public string TargetNamespace { get; }

        public IReadOnlyList<NamedTypeSymbolDescriptor> ContainedTypes => _containedTypes;

        private TargetAttributeDescriptor(List<string> relatedTypes, List<NamedTypeSymbolDescriptor> containedTypes, string targetNamespace)
        {
            _relatedTypes = relatedTypes;
            _containedTypes = containedTypes;
            TargetNamespace = targetNamespace;
        }

        public static TargetAttributeDescriptor? Create(INamedTypeSymbol symbol)
        {
            ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
            foreach (AttributeData attrib in attributes)
            {
                if (attrib.AttributeClass?.ToDisplayString() == AttributeNamesConstants.TargetAttributeFullyQualifiedName)
                {
                    ImmutableArray<KeyValuePair<string, TypedConstant>> arguments = attrib.NamedArguments;
                    TypedConstant constants = arguments[0].Value;
                    if (constants.IsNull)
                    {
                        continue;
                    }
                    ImmutableArray<TypedConstant> types = constants.Values;
                    List<string> relatedTypes = new(types.Length);
                    List<NamedTypeSymbolDescriptor> containedTypes = new(types.Length);
                    foreach (TypedConstant type in types)
                    {
                        INamedTypeSymbol currentType = (INamedTypeSymbol)type.Value!;
                        string name = currentType.ToDisplayString();
                        if (relatedTypes.Contains(name))
                        {
                            continue;
                        }

                        NamedTypeSymbolDescriptor? containedDescriptor = NamedTypeSymbolDescriptor.Create(currentType);
                        if (containedDescriptor is null)
                        {
                            continue;
                        }
                        containedTypes.Add(containedDescriptor);
                        relatedTypes.Add(name);
                    }
                    
                    TargetAttributeDescriptor descriptor = new(relatedTypes, containedTypes, symbol.ContainingNamespace.ToDisplayString());
                    return descriptor;
                }
            }

            return null;
        }

        public bool Equals(TargetAttributeDescriptor other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            bool sameNamespace = TargetNamespace == other.TargetNamespace;
            if (!sameNamespace)
            {
                return false;
            }

            bool sameTypes = RelatedTypes.SequenceEqual(other.RelatedTypes);
            if (!sameTypes)
            {
                return false;
            }

            bool sameMembers = ContainedTypes.SequenceEqual(other.ContainedTypes);
            return sameMembers;
        }
    }
}
