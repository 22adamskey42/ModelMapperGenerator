using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Descriptors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using static ModelMapperGenerator.Constants.SourceElementsConstants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class GenericClassCompilationBuilder
    {
        public static void BuildCompilationElements(Dictionary<string, List<NamedTypeSymbolDescriptor>>? symbols, TargetAttributeDescriptor targetDescriptor, ref SourceProductionContext context)
        {
            if (symbols is null || symbols.Count == 0)
            {
                return;
            }

            foreach (string symbolKey in symbols.Keys)
            {
                List<NamedTypeSymbolDescriptor> values = symbols[symbolKey];
                NamedTypeSymbolDescriptor firstSymbol = values[0];
                int typeArgsCount = firstSymbol.Symbol.TypeArguments.Length;
                string generatedGenericModelName = BuildGenericModelName(typeArgsCount, firstSymbol.SymbolName);
                string generatedModelName = firstSymbol.SymbolName + Model;
                string generatedMapperName = firstSymbol.SymbolName + Mapper;
                string modelString = ModelStringBuilder.BuildModelString(firstSymbol, targetDescriptor, generatedGenericModelName);
                string mapperString = GenerateMapperString(values, targetDescriptor, generatedMapperName);
                string modelFileName = FilenameBuilder.BuildFileName(generatedModelName, firstSymbol, targetDescriptor);
                string mapperFileName = FilenameBuilder.BuildFileName(generatedMapperName, firstSymbol, targetDescriptor);
                context.AddSource(modelFileName, modelString);
                context.AddSource(mapperFileName, mapperString);
            }
        }

        private static string GenerateMapperString(List<NamedTypeSymbolDescriptor> values, TargetAttributeDescriptor targetDescriptor, string generatedMapperName)
        {
            NamedTypeSymbolDescriptor firstSymbol = values[0];
            StringBuilder mapperStringBuilder = new();
            GenerateMapperClassStartString(mapperStringBuilder, firstSymbol, targetDescriptor, generatedMapperName);
            foreach (NamedTypeSymbolDescriptor descriptor in values)
            {
                CreateMethods(mapperStringBuilder, descriptor, targetDescriptor);
            }
            string generatedMapperString = GenerateMapperClassEndString(mapperStringBuilder);

            return generatedMapperString;
        }

        private static string GenerateMapperClassEndString(StringBuilder mapperBuilder)
        {
            mapperBuilder.Append(Indent).Append(ClosingBrace)
                .AppendLine().Append(ClosingBrace);

            return mapperBuilder.ToString();
        }

        private static void CreateMethods(StringBuilder mapperStringBuilder, NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetDescriptor)
        {
            (StringBuilder toModelBuilder, StringBuilder toDomainBuilder) = MapperStringBuilder.PopulateMapperStringBuilders(sourceDescriptor, targetDescriptor);

            string domainNameToUse = CreateTypeName(sourceDescriptor, targetDescriptor, false);
            string modelNameTouse = CreateTypeName(sourceDescriptor, targetDescriptor, true);

            string methodsString = $$"""

                        public static {{modelNameTouse}} ToModel(this {{domainNameToUse}} value)
                        {
                            {{modelNameTouse}} model = new {{modelNameTouse}}()
                            {
                {{toModelBuilder}}
                            };
                
                            return model;
                        }
                
                        public static {{domainNameToUse}} ToDomain(this {{modelNameTouse}} value)
                        {
                            {{domainNameToUse}} domain = new {{domainNameToUse}}()
                            {
                {{toDomainBuilder}}
                            };
                
                            return domain;
                        }

                """;

            mapperStringBuilder.Append(methodsString);
        }


        private static void GenerateMapperClassStartString(StringBuilder mapperStringBuilder, NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetDescriptor, string generatedMapperName)
        {
            string startString = $$"""
                using {{sourceDescriptor.SymbolNamespaceName}};
                
                namespace {{targetDescriptor.TargetNamespace}}
                {
                    public static class {{generatedMapperName}}
                    {
                """;
            mapperStringBuilder.Append(startString);
        }

        private static string BuildGenericModelName(int argCount, string symbolName)
        {
            StringBuilder builder = new();
            builder.Append(symbolName).Append(Model).Append(LesserThan);
            for (int i = 0; i < argCount; i++)
            {
                builder.Append(GenericT).Append(i);
                if (i < argCount - 1)
                {
                    builder.Append(Comma);
                }
            }
            builder.Append(GreaterThan);

            return builder.ToString();

        }

        private static string CreateTypeName(NamedTypeSymbolDescriptor descriptor, TargetAttributeDescriptor targetDescriptor, bool isModel = false)
        {
            StringBuilder? nameBuilder = null;
            if (descriptor.Symbol.IsGenericType)
            {
                ImmutableArray<ITypeSymbol> args = descriptor.Symbol.TypeArguments;
                int argLen = args.Length;
                nameBuilder = new StringBuilder();
                nameBuilder.Append(descriptor.SymbolName);
                if (isModel)
                {
                    nameBuilder.Append(Model);
                }
                nameBuilder.Append(LesserThan);
                for (int i = 0; i < argLen; i++)
                {
                    string currentArgName = args[i].ToDisplayString();
                    if (isModel)
                    {
                        bool isRelatedType = targetDescriptor.RelatedTypes.Contains(currentArgName);
                        if (isRelatedType)
                        {
                            nameBuilder.Append(targetDescriptor.TargetNamespace).Append(Dot).Append(args[i].Name).Append(Model);
                        }
                        else
                        {
                            nameBuilder.Append(currentArgName);
                        }
                    }
                    else
                    {
                        nameBuilder.Append(currentArgName);
                    }

                    if (i + 1 < argLen)
                    {
                        nameBuilder.Append(Comma).Append(Whitespace);
                    }
                }
                nameBuilder.Append(GreaterThan);
            }

            string domainNameToUse = nameBuilder is not null ? nameBuilder.ToString() : descriptor.SymbolName;
            return domainNameToUse;
        }
    }
}
