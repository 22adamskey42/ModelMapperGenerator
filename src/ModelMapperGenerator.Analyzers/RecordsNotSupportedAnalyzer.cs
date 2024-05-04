using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using ModelMapperGenerator.Analyzer.Constants;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using ModelMapperGenerator.Analyzers.Infrastructure;

namespace ModelMapperGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class RecordsNotSupportedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _descriptor = new(
            id: RecordsNotSupportedAnalyzerConstants.DiagnosticId,
            title: RecordsNotSupportedAnalyzerConstants.Title,
            messageFormat: RecordsNotSupportedAnalyzerConstants.MessageFormat,
            category: RecordsNotSupportedAnalyzerConstants.Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: RecordsNotSupportedAnalyzerConstants.Description);

        private static readonly ImmutableArray<DiagnosticDescriptor> _diagnosticDescriptors = [_descriptor];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _diagnosticDescriptors;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(syntaxNodeAnalysisContext =>
            {
                bool isTarget = AnalyzerHelpers.IsTargetAttribute(ref syntaxNodeAnalysisContext);

                if (!isTarget)
                {
                    return;
                }

                AttributeSyntax node = (AttributeSyntax)syntaxNodeAnalysisContext.Node;
                IEnumerable<SyntaxNode> descendants = node.DescendantNodes();

                foreach (var descendant in descendants)
                {
                    if (descendant is not TypeOfExpressionSyntax)
                    {
                        continue;
                    }

                    TypeOfExpressionSyntax typeOfSyntax = (TypeOfExpressionSyntax)descendant;
                    SymbolInfo symbolInfo = syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(typeOfSyntax.Type);
                    if (symbolInfo.Symbol is null)
                    {
                        return;
                    }

                    if (symbolInfo.Symbol is INamedTypeSymbol namedType && namedType.IsRecord)
                    {
                        ReportDiagnostic(ref syntaxNodeAnalysisContext, typeOfSyntax.GetLocation(), namedType);
                    }
                }
            }, SyntaxKind.Attribute);
        }

        private static void ReportDiagnostic(ref SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Location location, INamedTypeSymbol symbol)
        {
            Diagnostic diag = Diagnostic.Create(_descriptor, location, symbol.ToDisplayString());
            syntaxNodeAnalysisContext.ReportDiagnostic(diag);
        }
    }
}
