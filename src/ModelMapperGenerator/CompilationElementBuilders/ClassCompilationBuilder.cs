using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Descriptors;
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
            string modelString = ModelStringBuilder.BuildModelString(symbolDescriptor, targetDescriptor, generatedModelName);
            string mapperString = GenerateMapperString(symbolDescriptor, targetDescriptor, generatedMapperName);
            string modelFileName = FilenameBuilder.BuildFileName(generatedModelName, symbolDescriptor, targetDescriptor);
            string mapperFileName = FilenameBuilder.BuildFileName(generatedMapperName, symbolDescriptor, targetDescriptor);
            context.AddSource(modelFileName, modelString);
            context.AddSource(mapperFileName, mapperString);
        }

        private static string GenerateMapperString(NamedTypeSymbolDescriptor descriptor, TargetAttributeDescriptor targetDescriptor, string generatedMapperName)
        {
            (StringBuilder toModelBuilder, StringBuilder toDomainBuilder) = MapperStringBuilder.PopulateMapperStringBuilders(descriptor, targetDescriptor);
            
            string modelName = descriptor.SymbolName + Model;

            string code = $$"""
                using {{descriptor.SymbolNamespaceName}};

                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public static class {{generatedMapperName}}
                    {
                        public static {{modelName}} ToModel(this {{descriptor.SymbolName}} value)
                        {
                            {{modelName}} model = new {{modelName}}()
                            {
                {{toModelBuilder}}
                            };

                            return model;
                        }

                        public static {{descriptor.SymbolName}} ToDomain(this {{modelName}} value)
                        {
                            {{descriptor.SymbolName}} domain = new {{descriptor.SymbolName}}()
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
    }
}
