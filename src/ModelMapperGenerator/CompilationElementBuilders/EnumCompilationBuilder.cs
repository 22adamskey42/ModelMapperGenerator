using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Descriptors;
using System;
using System.Text;
using static ModelMapperGenerator.Constants.SourceElementsConstants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class EnumCompilationBuilder
    {
        public static void BuildCompilationElements(NamedTypeSymbolDescriptor descriptor, TargetAttributeDescriptor targetDescriptor, ref SourceProductionContext context)
        {
            string generatedModelName = descriptor.SymbolName + Model;
            string generatedMapperName = descriptor.SymbolName + Mapper;
            string modelString = GenerateModelString(descriptor, targetDescriptor, generatedModelName);
            string mapperString = GenerateMapperString(descriptor, targetDescriptor, generatedMapperName);
            string modelFileName = FilenameBuilder.BuildFileName(generatedModelName, descriptor, targetDescriptor);
            string mapperFileName = FilenameBuilder.BuildFileName(generatedMapperName, descriptor, targetDescriptor);
            context.AddSource(modelFileName, modelString);
            context.AddSource(mapperFileName, mapperString);
        }

        private static string GenerateModelString(NamedTypeSymbolDescriptor descriptor, TargetAttributeDescriptor targetDescriptor, string generatedModelName)
        {
            StringBuilder builder = new();
            Span<FieldSymbolDescriptor> span = descriptor.EnumSymbolMembers.AsSpan();
            foreach (ref readonly FieldSymbolDescriptor member in span)
            {
                builder.Append(DoubleIndent).Append(member.Name);
                if (member.ConstantValue is not null)
                {
                    builder.Append(Assignment).Append(member.ConstantValue);
                }

                builder.Append(Comma).AppendLine();
            }

            string code = $$"""
                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public enum {{generatedModelName}}
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
            Span<FieldSymbolDescriptor> span = descriptor.EnumSymbolMembers.AsSpan();

            foreach (ref readonly FieldSymbolDescriptor member in span)
            {
                toModelBuilder.Append(QuadrupleIndent).Append(descriptor.SymbolName).Append(Dot).Append(member.Name).Append(Arrow).Append(modelName).Append(Dot).Append(member.Name).Append(Comma).AppendLine();
                toDomainBuilder.Append(QuadrupleIndent).Append(modelName).Append(Dot).Append(member.Name).Append(Arrow).Append(descriptor.SymbolName).Append(Dot).Append(member.Name).Append(Comma).AppendLine();
            }
            
            toModelBuilder.Append(UnknownEnum);
            toDomainBuilder.Append(UnknownEnum);
            string code = $$"""
                using System;
                using {{descriptor.SymbolNamespaceName}};
                
                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public static class {{generatedMapperName}}
                    {
                        public static {{modelName}} ToModel(this {{descriptor.SymbolName}} value)
                        {
                            return value switch
                            {
                {{toModelBuilder}}
                            };
                        }
                
                        public static {{descriptor.SymbolName}} ToDomain(this {{modelName}} value)
                        {
                            return value switch
                            {
                {{toDomainBuilder}}
                            };
                        }
                    }
                }
                """;

            return code;
        }
    }
}
