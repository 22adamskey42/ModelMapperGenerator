using System.Threading;
using System.Linq;
using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace ModelMapperGenerator
{
    [Generator]
    public class ModelMapperSourceGenerator : IIncrementalGenerator
    {
        private static readonly SymbolDescriptorComparer _comparer = new SymbolDescriptorComparer();

        public ModelMapperSourceGenerator()
        {
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<SymbolDescriptor?> provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (SyntaxNode node, CancellationToken ct) =>
                {
                    bool isEnum = node.GetType() == typeof(EnumDeclarationSyntax);
                    return isEnum;
                },
                transform: static (GeneratorSyntaxContext context, CancellationToken ct) =>
                {
                    INamedTypeSymbol? symbol = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(context.Node);
                    if (symbol is null)
                    {
                        return null;
                    }

                    IEnumerable<SyntaxTree> trees = context.SemanticModel.Compilation.SyntaxTrees;
                    SymbolDescriptor? result = AnalyzeSyntaxTrees(trees, symbol, ref ct);
                    return result;
                })
                .Where(x => x is not null)
                .WithComparer(_comparer);

            IncrementalValueProvider<ImmutableArray<SymbolDescriptor?>> collected = provider.Collect();

            context.RegisterSourceOutput(collected, Execute);
        }

        private static SymbolDescriptor? AnalyzeSyntaxTrees(IEnumerable<SyntaxTree> trees, INamedTypeSymbol symbol, ref CancellationToken cancellationToken)
        {
            foreach (SyntaxTree tree in trees)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                SyntaxNode root = tree.GetRoot(cancellationToken);
                IEnumerable<SyntaxNode> descendantNodes = root.DescendantNodes(_ => true);
                SymbolDescriptor? result = AnalyzeSyntaxNodes(descendantNodes, symbol, ref cancellationToken);
                if (result is not null)
                {
                    return result;
                }
            }
            
            return null;
        }

        private static SymbolDescriptor? AnalyzeSyntaxNodes(IEnumerable<SyntaxNode> syntaxNodes, INamedTypeSymbol symbol, ref CancellationToken cancellationToken)
        {
            foreach (SyntaxNode node in syntaxNodes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

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
                                SymbolDescriptor? result = AnalyzeAttributeArgumentList(ref arguments, symbol, attributeNode);
                                if (result is not null)
                                {
                                    return result;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static SymbolDescriptor? AnalyzeAttributeArgumentList(ref SeparatedSyntaxList<AttributeArgumentSyntax> argumentList, INamedTypeSymbol symbol, AttributeSyntax attributeNode)
        {
            foreach (AttributeArgumentSyntax argument in argumentList)
            {
                if (argument.Expression.GetType() == typeof(ArrayCreationExpressionSyntax))
                {
                    InitializerExpressionSyntax? initializer = ((ArrayCreationExpressionSyntax)argument.Expression).Initializer;
                    if (initializer is not null &&
                        initializer.Expressions.Count > 0 &&
                        initializer.Expressions[0].GetType() == typeof(TypeOfExpressionSyntax))
                    {
                        foreach (SyntaxNode expression in initializer.Expressions)
                        {
                            TypeOfExpressionSyntax expr = (TypeOfExpressionSyntax)expression;
                            string text = expr.Type.ToString();
                            string symbolText = symbol.ToDisplayString();
                            if (text == symbolText)
                            {
                                string attributeNamespace = FindNodeNamespace(attributeNode);
                                SymbolDescriptor desc = new(symbol, attributeNamespace);
                                return desc;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static string FindNodeNamespace(AttributeSyntax node)
        {
            SyntaxNode currentNode = node;
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

        private void Execute(SourceProductionContext context, ImmutableArray<SymbolDescriptor?> symbols)
        {
            foreach (SymbolDescriptor? symbol in symbols)
            {
                if (symbol is null)
                {
                    continue;
                }

                BuildCompilationElements(symbol, ref context);
            }
        }

        private void BuildCompilationElements(SymbolDescriptor descriptor, ref SourceProductionContext context)
        {
            string enumNamespaceName = descriptor.SymbolNamespace;
            string generatedModelName = descriptor.SymbolName + "Model";
            string generatedMapperName = descriptor.SymbolName + "Mapper";
            ImmutableArray<ISymbol> members = descriptor.Symbol.GetMembers();
            string modelString = GenerateModelString(descriptor.TargetNamespace, generatedModelName, ref members);
            string mapperString = GenerateMapperString(descriptor.TargetNamespace, descriptor.SymbolName, enumNamespaceName, generatedMapperName, ref members);
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
