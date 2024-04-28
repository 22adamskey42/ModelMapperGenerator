using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Descriptors;
using System;
using System.Text;
using static ModelMapperGenerator.Constants.SourceElementsConstants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class MapperStringBuilder
    {
        public static (StringBuilder toModelBuilder, StringBuilder toDomainBuilder) PopulateMapperStringBuilders(NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetDescriptor)
        {
            StringBuilder toModelBuilder = new();
            StringBuilder toDomainBuilder = new();
            Span<PropertySymbolDescriptor> span = sourceDescriptor.ClassSymbolMembers.AsSpan();
            foreach (ref PropertySymbolDescriptor property in span)
            {
                if (!property.HasPublicGetter)
                {
                    continue;
                }

                bool propertyTypeIsRelatedTypeInNamespace = property.ReturnTypeIsRelatedTypeInNamespace(targetDescriptor.TargetNamespace);
                bool shouldBeNullable = property.IsNullable || property.ReturnTypeKind == TypeKind.Class;
                toModelBuilder.Append(QuadrupleIndent).Append(property.PropertyName).Append(Assignment).Append(Value).Append(property.PropertyName);

                if (propertyTypeIsRelatedTypeInNamespace)
                {
                    toModelBuilder.Append(shouldBeNullable ? ToModelNullSafe : ToModel);
                }

                toModelBuilder.Append(Comma).AppendLine();

                if (property.HasPublicSetter)
                {
                    toDomainBuilder.Append(QuadrupleIndent).Append(property.PropertyName).Append(Assignment).Append(Value).Append(property.PropertyName);
                    if (propertyTypeIsRelatedTypeInNamespace)
                    {
                        toDomainBuilder.Append(shouldBeNullable ? ToDomainNullSafe : ToDomain);
                    }

                    toDomainBuilder.Append(Comma).AppendLine();
                }
            }

            return (toModelBuilder, toDomainBuilder);
        }
    }
}
