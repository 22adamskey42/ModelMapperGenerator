using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ModelMapperGenerator.Analyzer.Constants;
using ModelMapperGenerator.Analyzers.Descriptors;
using ModelMapperGenerator.Analyzers.Infrastructure;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ModelMapperGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class TypesWithSameNameNotAllowedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly TypeNameDescriptorComparer _comparer = new();
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

                Dictionary<TypeNameDescriptor, List<Location>> dictionary = new(_comparer);

                AttributeSyntax node = (AttributeSyntax)syntaxNodeContext.Node;
                IEnumerable<SyntaxNode> descendants = node.DescendantNodes();

                foreach (SyntaxNode descendant in descendants)
                {
                    if (descendant is not TypeOfExpressionSyntax)
                    {
                        continue;
                    }

                    TypeNameDescriptor typeName = GetTypeNameFromTypeOfSyntaxNode(descendant);
                    if (!typeName.HasValue)
                    {
                        continue;
                    }

                    TypeNameDescriptor key = dictionary.Keys.FirstOrDefault(x => x == typeName);

                    Location location = descendant.GetLocation();
                    if (key.HasValue)
                    {
                        dictionary[key].Add(location);
                    }
                    else
                    {
                        dictionary[typeName] = [location];
                    }
                }

                foreach (TypeNameDescriptor key in dictionary.Keys)
                {
                    if (dictionary[key].Count >= 2)
                    {
                        foreach (Location location in dictionary[key])
                        {
                            ReportDiagnostic(ref syntaxNodeContext, location, key.TypeName!);
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

        private TypeNameDescriptor GetTypeNameFromTypeOfSyntaxNode(SyntaxNode node)
        {
            TypeOfExpressionSyntax typeOfSyntax = (TypeOfExpressionSyntax)node;
            TypeSyntax type = typeOfSyntax.Type;

            TypeNameDescriptor desc = type switch
            {
                QualifiedNameSyntax qualifiedName => new(qualifiedName.Right.Identifier.Text),
                IdentifierNameSyntax identifierName => new(identifierName.Identifier.Text),
                GenericNameSyntax genericName => new(genericName.Identifier.Text, genericName.TypeArgumentList.ToFullString()),
                _ => new()
            };

            return desc;
        }
    }
}
