using ModelMapperGenerator.Descriptors;
using System;
using System.Text;
using static ModelMapperGenerator.Constants.SourceElementsConstants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class ModelStringBuilder
    {
        public static string BuildModelString(NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetDescriptor, string generatedModelName)
        {
            StringBuilder builder = new();
            Span<PropertySymbolDescriptor> span = sourceDescriptor.ClassSymbolMembers.AsSpan();
            int genericParamCount = 0;
            foreach (ref PropertySymbolDescriptor property in span)
            {
                if (!property.HasPublicGetter)
                {
                    continue;
                }

                builder.Append(DoubleIndent).Append(Public);
                if (property.OriginalDefinitionGeneric)
                {
                    builder.Append(GenericT).Append(genericParamCount);
                    genericParamCount++;
                }
                else
                {
                    bool propertyTypeIsRelatedTypeInNamespace = property.ReturnTypeIsRelatedTypeInNamespace(targetDescriptor.TargetNamespace);

                    if (propertyTypeIsRelatedTypeInNamespace)
                    {
                        builder.Append(targetDescriptor.TargetNamespace).Append(Dot).Append(property.ReturnTypeName).Append(Model);
                    }
                    else
                    {
                        builder.Append(property.ReturnTypeFullyQualifiedName);
                    }

                    if (property.IsNullable)
                    {
                        builder.Append(QuestionMark);
                    }
                }

                builder.Append(Whitespace).Append(property.PropertyName).Append(Whitespace).Append(GetSet).AppendLine();
            }

            string code = $$"""
                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public class {{generatedModelName}}
                    {
                {{builder}}
                    }
                }
                """;

            return code;
        }
    }
}