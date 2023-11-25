using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Descriptors;
using System;
using System.Collections.Immutable;
using System.Text;
using static ModelMapperGenerator.Constants.SourceElementsConstants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class ClassCompilationBuilder
    {
        public static void BuildCompilationElements(NamedTypeSymbolDescriptor symbolDescriptor, TargetAttributeDescriptor targetDescriptor, ref SourceProductionContext context)
        {
            string generatedModelName = symbolDescriptor.SymbolName + Model;
            string generatedMapperName = symbolDescriptor.SymbolName + Mapper;
            string modelString = GenerateModelString(symbolDescriptor, targetDescriptor, generatedModelName);
            string mapperString = GenerateMapperString(symbolDescriptor, targetDescriptor, generatedMapperName);
            string modelFileName = FilenameBuilder.BuildFileName(generatedModelName, symbolDescriptor, targetDescriptor);
            string mapperFileName = FilenameBuilder.BuildFileName(generatedMapperName, symbolDescriptor, targetDescriptor);
            context.AddSource(modelFileName, modelString);
            context.AddSource(mapperFileName, mapperString);
        }

        private static string GenerateModelString(NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetDescriptor, string generatedModelName)
        {
            StringBuilder builder = new();
            Span<PropertySymbolDescriptor> span = sourceDescriptor.ClassSymbolMembers.AsSpan();

            foreach (ref PropertySymbolDescriptor property in span)
            {
                if (!property.HasPublicGetter)
                {
                    continue;
                }

                builder.Append(DoubleIndent).Append(Public);
                bool propertyTypeIsRelatedTypeInNamespace = property.ReturnTypeIsRelatedTypeInNamespace(targetDescriptor.TargetNamespace);
                
                if (propertyTypeIsRelatedTypeInNamespace)
                {
                    builder.Append(targetDescriptor.TargetNamespace).Append(Dot).Append(property.ReturnTypeName).Append(Model);
                }
                else
                {
                    builder.Append(property.ReturnTypeFullyQualifiedName);
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

        private static string GenerateMapperString(NamedTypeSymbolDescriptor descriptor, TargetAttributeDescriptor targetDescriptor, string generatedMapperName)
        {
            StringBuilder toModelBuilder = new();
            StringBuilder toDomainBuilder = new();
            string modelName = descriptor.SymbolName + Model;

            Span<PropertySymbolDescriptor> span = descriptor.ClassSymbolMembers.AsSpan();
            foreach (ref PropertySymbolDescriptor property in span)
            {
                if (!property.HasPublicGetter)
                {
                    continue;
                }

                bool propertyTypeIsRelatedTypeInNamespace = property.ReturnTypeIsRelatedTypeInNamespace(targetDescriptor.TargetNamespace);
                toModelBuilder.Append(QuadrupleIndent).Append(property.PropertyName).Append(Assignment).Append(Value).Append(property.PropertyName);

                if (propertyTypeIsRelatedTypeInNamespace)
                {
                    toModelBuilder.Append(ToModel);
                }

                toModelBuilder.Append(Comma).AppendLine();

                if (property.HasPublicSetter)
                {
                    toDomainBuilder.Append(QuadrupleIndent).Append(property.PropertyName).Append(Assignment).Append(Value).Append(property.PropertyName);
                    if (propertyTypeIsRelatedTypeInNamespace)
                    {
                        toDomainBuilder.Append(ToDomain);
                    }

                    toDomainBuilder.Append(Comma).AppendLine();
                }
            }

            string domainNameToUse = CreateTypeName(descriptor);

            string code = $$"""
                using {{descriptor.SymbolNamespaceName}};

                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public static class {{generatedMapperName}}
                    {
                        public static {{modelName}} ToModel(this {{domainNameToUse}} value)
                        {
                            {{modelName}} model = new {{modelName}}()
                            {
                {{toModelBuilder}}
                            };

                            return model;
                        }

                        public static {{domainNameToUse}} ToDomain(this {{modelName}} value)
                        {
                            {{domainNameToUse}} domain = new {{domainNameToUse}}()
                            {
                {{toDomainBuilder}}
                            };

                            return domain;
                        }
                    }
                }
                """;

            return code;
        }

        private static string CreateTypeName(NamedTypeSymbolDescriptor descriptor)
        {
            StringBuilder? domainNameBuilder = null;
            if (descriptor.Symbol.IsGenericType)
            {
                ImmutableArray<ITypeSymbol> args = descriptor.Symbol.TypeArguments;
                int argLen = args.Length;
                domainNameBuilder = new StringBuilder();
                domainNameBuilder.Append(descriptor.SymbolName);
                domainNameBuilder.Append(LesserThan);
                for (int i = 0; i < argLen; i++)
                {
                    domainNameBuilder.Append(args[i].ToDisplayString());
                    if (i + 1 < argLen)
                    {
                        domainNameBuilder.Append(Comma).Append(Whitespace);
                    }
                }
                domainNameBuilder.Append(GreaterThan);
            }

            string domainNameToUse = domainNameBuilder is not null ? domainNameBuilder.ToString() : descriptor.SymbolName;
            return domainNameToUse;
        }
    }
}
