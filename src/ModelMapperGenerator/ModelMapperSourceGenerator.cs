using System.Threading;
using System.Linq;
using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Buffers;

namespace ModelMapperGenerator
{
    [Generator]
    public class ModelMapperSourceGenerator : IIncrementalGenerator
    {

        public ModelMapperSourceGenerator()
        {
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AttributeSyntax> provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (SyntaxNode node, CancellationToken ct) =>
                {
                    bool isAttribute = node.GetType() == typeof(AttributeSyntax);
                    if (isAttribute)
                    {
                        AttributeSyntax attributeNode = (AttributeSyntax)node;
                        if (attributeNode.Name.GetType() == typeof(IdentifierNameSyntax))
                        {
                            bool isTargetAttribute = ((IdentifierNameSyntax)attributeNode.Name).Identifier.Text == "ModelGenerationTarget";
                            if (isTargetAttribute)
                            {
                                if (attributeNode.ArgumentList is not null)
                                {
                                    SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attributeNode.ArgumentList.Arguments;
                                    foreach (AttributeArgumentSyntax argument in arguments)
                                    {
                                        if (argument.Expression.GetType() == typeof(ArrayCreationExpressionSyntax))
                                        {
                                            InitializerExpressionSyntax? initializer = ((ArrayCreationExpressionSyntax)argument.Expression).Initializer;
                                            if (initializer is not null &&
                                                initializer.Expressions.Count > 0 &&
                                                initializer.Expressions[0].GetType() == typeof(TypeOfExpressionSyntax))
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return false;
                },
                transform: static (GeneratorSyntaxContext context, CancellationToken ct) =>
                {
                    return (AttributeSyntax)context.Node;
                }).Where(x => x is not null);


            IncrementalValueProvider<ImmutableArray<AttributeSyntax>> collected = provider.Collect();
            IncrementalValueProvider<(Compilation Left, ImmutableArray<AttributeSyntax> Right)> compilation = context.CompilationProvider.Combine(collected);
            context.RegisterSourceOutput(compilation, Execute);
        }

        private void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<AttributeSyntax> Right) compilation)
        {
            ArrayPool<string> sharedPool = ArrayPool<string>.Shared;
            foreach (AttributeSyntax attributeSyntax in compilation.Right)
            {
                string[]? rentedArray = null;
                try
                {
                    InitializerExpressionSyntax? initializer = ExtractInitializer(attributeSyntax);
                    if (initializer is null)
                    {
                        continue;
                    }
                    int count = initializer.Expressions.Count;
                    rentedArray = sharedPool.Rent(count);
                    ExtractTypeNames(initializer, rentedArray);
                    string targetNamespace = FindNodeNamespace(attributeSyntax);
                    foreach (string typeName in rentedArray)
                    {
                        BuildCompilationElements(targetNamespace, typeName, compilation.Left, ref context);
                    }

                }
                finally
                {
                    sharedPool.Return(rentedArray);
                }
            }
        }

        private InitializerExpressionSyntax? ExtractInitializer(AttributeSyntax attributeSyntax)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                return null;
            }

            SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attributeSyntax.ArgumentList.Arguments;
            foreach (AttributeArgumentSyntax argument in arguments)
            {
                if (argument.Expression.GetType() == typeof(ArrayCreationExpressionSyntax))
                {
                    InitializerExpressionSyntax? initializer = ((ArrayCreationExpressionSyntax)argument.Expression).Initializer;
                    if (initializer is null)
                    {
                        continue;
                    }

                    return initializer;
                }
            }

            return null;
        }

        private void ExtractTypeNames(InitializerExpressionSyntax initializer, string[] buffer)
        {
            int position = 0;
            foreach (ExpressionSyntax expression in initializer.Expressions)
            {
                if (expression.GetType() == typeof(TypeOfExpressionSyntax))
                {
                    TypeOfExpressionSyntax typeOfExpressionSyntax = (TypeOfExpressionSyntax)expression;
                    string typeOfExpressionValue = typeOfExpressionSyntax.Type.ToFullString();
                    buffer[position] = typeOfExpressionValue;
                    position++;
                }
            }
        }

        private string FindNodeNamespace(AttributeSyntax node)
        {
            SyntaxNode currentNode = (SyntaxNode)node;
            while (currentNode.Parent is not null)
            {
                if (currentNode.Parent is NamespaceDeclarationSyntax ns)
                {
                    string name = ns.Name.ToString();
                    return name;
                }
                currentNode = currentNode.Parent;
            }

            throw new InvalidOperationException("This should not happen");
        }

        private void BuildCompilationElements(string targetNamespace, string typeName, Compilation compilation, ref SourceProductionContext context)
        {
            if (typeName is null || string.IsNullOrWhiteSpace(targetNamespace) || string.IsNullOrWhiteSpace(typeName))
            {
                return;
            }

            INamedTypeSymbol? enumSymbol = compilation.GetTypeByMetadataName(typeName);
            if (enumSymbol is null)
            {
                return;
            }

            string enumNamespaceName = enumSymbol.ContainingNamespace.ToDisplayString();
            string generatedModelName = enumSymbol.Name + "Model";
            string generatedMapperName = enumSymbol.Name + "Mapper";
            ImmutableArray<ISymbol> members = enumSymbol.GetMembers();
            string modelString = GenerateModelString(targetNamespace, generatedModelName, ref members);
            string mapperString = GenerateMapperString(targetNamespace, enumSymbol.Name, enumNamespaceName, generatedMapperName, ref members);
            context.AddSource($"{generatedModelName}.g.cs", modelString);
            context.AddSource($"{generatedMapperName}.g.cs", mapperString);
        }

        private string GenerateModelString(string targetNamespace, string generatedModelName, ref ImmutableArray<ISymbol> members)
        {
            StringBuilder builder = new();
            foreach (ISymbol member in members)
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    if (fieldSymbol.HasConstantValue)
                    {
                        builder.Append("    ").Append(fieldSymbol.Name).Append(" = ").Append(fieldSymbol.ConstantValue);
                    }
                    else
                    {
                        builder.Append("    ").Append(fieldSymbol.Name);
                    }

                    builder.Append(',').AppendLine();
                }
            }

            string code = $$"""
                namespace {{targetNamespace}};

                public enum {{generatedModelName}}
                {
                {{builder}}}
                """;

            return code;
        }

        private string GenerateMapperString(string targetNamespace, string enumName, string enumNamespaceName, string generatedMapperName, ref ImmutableArray<ISymbol> members)
        {
            StringBuilder toModelBuilder = new();
            StringBuilder toDomainBuilder = new();
            string modelName = enumName + "Model";
            foreach (ISymbol member in members)
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    toModelBuilder.Append("            ").Append(enumName).Append('.').Append(fieldSymbol.Name).Append(" => ").Append(modelName).Append('.').Append(fieldSymbol.Name).Append(',').AppendLine();
                    toDomainBuilder.Append("            ").Append(modelName).Append('.').Append(fieldSymbol.Name).Append(" => ").Append(enumName).Append('.').Append(fieldSymbol.Name).Append(',').AppendLine();
                }
            }
            string unknown = "            _ => throw new ArgumentOutOfRangeException(\"Unknown enum value.\")";
            toModelBuilder.Append(unknown);
            toDomainBuilder.Append(unknown);

            string code = $$"""
                using System;
                using {{enumNamespaceName}};
                
                namespace {{targetNamespace}};

                public static class {{generatedMapperName}}
                {
                    public static {{modelName}} ToModel(this {{enumName}} value)
                    {
                        return value switch
                        {
                {{toModelBuilder}}
                        };
                    }

                    public static {{enumName}} ToDomain(this {{modelName}} value)
                    {
                        return value switch
                        {
                {{toDomainBuilder}}
                        };
                    }
                }
                """;

            return code;
        }
    }
}
