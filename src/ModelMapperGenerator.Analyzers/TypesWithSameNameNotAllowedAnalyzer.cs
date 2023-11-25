using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ModelMapperGenerator.Analyzer.Constants;
using ModelMapperGenerator.Analyzers.Infrastructure;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class TypesWithSameNameNotAllowedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _descriptor = new(
            id: TypesWithSameNameNotAllowedAnalyzerConstants.DiagnosticId,
            title: TypesWithSameNameNotAllowedAnalyzerConstants.Title,
            messageFormat: TypesWithSameNameNotAllowedAnalyzerConstants.MessageFormat,
            category: TypesWithSameNameNotAllowedAnalyzerConstants.Category,
            description: TypesWithSameNameNotAllowedAnalyzerConstants.Description,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = [_descriptor];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                bool isTarget = AnalyzerHelpers.IsTargetAttribute(ref syntaxNodeContext);

                if (!isTarget)
                {
                    return;
                }

                Dictionary<string, List<Location>> dictionary = new();

                AttributeSyntax node = (AttributeSyntax)syntaxNodeContext.Node;
                IEnumerable<SyntaxNode> descendants = node.DescendantNodes();

                foreach (SyntaxNode descendant in descendants)
                {
                    if (descendant is not TypeOfExpressionSyntax)
                    {
                        continue;
                    }

                    string? typeName = GetTypeNameFromTypeOfSyntaxNode(descendant);
                    if (typeName is null)
                    {
                        continue;
                    }

                    bool hasKey = dictionary.ContainsKey(typeName);
                    Location location = descendant.GetLocation();
                    if (hasKey)
                    {
                        dictionary[typeName].Add(location);
                    }
                    else
                    {
                        dictionary[typeName] = new() { location };
                    }
                }

                foreach (string key in dictionary.Keys)
                {
                    if (dictionary[key].Count < 2)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (Location location in dictionary[key])
                        {
                            ReportDiagnostic(ref syntaxNodeContext, location, key);
                        }
                    }
                }

            }, SyntaxKind.Attribute);
        }

        private static void ReportDiagnostic(ref SyntaxNodeAnalysisContext context, Location location, string symbolName)
        {
            Diagnostic diagnostic = Diagnostic.Create(_descriptor, location, symbolName);
            context.ReportDiagnostic(diagnostic);
        }

        private string? GetTypeNameFromTypeOfSyntaxNode(SyntaxNode node)
        {
            TypeOfExpressionSyntax typeOfSyntax = (TypeOfExpressionSyntax)node;
            TypeSyntax type = typeOfSyntax.Type;
            if (type is QualifiedNameSyntax qualifiedName)
            {
                SyntaxToken identifier = qualifiedName.Right.Identifier;
                return identifier.Text;
            }
            else if (type is IdentifierNameSyntax identifierName)
            {
                SyntaxToken identifier = identifierName.Identifier;
                return identifier.Text;
            }
            return null;
        }
    }
}
