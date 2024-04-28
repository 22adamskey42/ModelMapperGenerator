using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ModelMapperGenerator.Analyzers.Constants;
using ModelMapperGenerator.Analyzers.Infrastructure;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace ModelMapperGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class OpenGenericsNotSupportedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _descriptor = new(
            id: OpenGenericsNotSupportedAnalyzerConstants.DiagnosticId,
            description: OpenGenericsNotSupportedAnalyzerConstants.Description,
            messageFormat: OpenGenericsNotSupportedAnalyzerConstants.MessageFormat,
            title: OpenGenericsNotSupportedAnalyzerConstants.Title,
            category: OpenGenericsNotSupportedAnalyzerConstants.Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly ImmutableArray<DiagnosticDescriptor> _diagnosticDescriptors = [_descriptor];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _diagnosticDescriptors;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(context =>
            {
                bool isTarget = AnalyzerHelpers.IsTargetAttribute(ref context);
                if (!isTarget)
                {
                    return;
                }

                AttributeSyntax node = (AttributeSyntax)context.Node;
                IEnumerable<SyntaxNode> descendants = node.DescendantNodes();

                foreach (var descendant in descendants)
                {
                    if (descendant is not TypeOfExpressionSyntax)
                    {
                        continue;
                    }

                    TypeOfExpressionSyntax typeOfSyntax = (TypeOfExpressionSyntax)descendant;
                    SyntaxNode omitted = typeOfSyntax.DescendantNodes().FirstOrDefault(x => x is OmittedTypeArgumentSyntax);
                    if (omitted is not null)
                    {
                        Location location = typeOfSyntax.GetLocation();
                        ReportDiagnostic(ref context, location);
                    }
                }
            }, SyntaxKind.Attribute);
        }

        private static void ReportDiagnostic(ref SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Location location)
        {
            Diagnostic diag = Diagnostic.Create(_descriptor, location);
            syntaxNodeAnalysisContext.ReportDiagnostic(diag);
        }
    }
}
