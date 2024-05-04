using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ModelMapperGenerator.Analyzer.Constants;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ModelMapperGenerator.Analyzers.UnitTests")]

namespace ModelMapperGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class TargetAttributeUsageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _descriptor = new(
            id: TargetAttributeUsageAnalyzerConstants.DiagnosticId, 
            title: TargetAttributeUsageAnalyzerConstants.Title, 
            messageFormat: TargetAttributeUsageAnalyzerConstants.MessageFormat, 
            category: TargetAttributeUsageAnalyzerConstants.Category,
            defaultSeverity: DiagnosticSeverity.Error, 
            isEnabledByDefault: true,
            description: TargetAttributeUsageAnalyzerConstants.Description);

        private static readonly ImmutableArray<DiagnosticDescriptor> _diagnosticDescriptors = [_descriptor];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _diagnosticDescriptors;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(namespaceContext =>
            {
                INamespaceSymbol namespaceSymbol = (INamespaceSymbol)namespaceContext.Symbol;
                ImmutableArray<INamedTypeSymbol> typeMembers = namespaceSymbol.GetTypeMembers();
#pragma warning disable IDE0028 // Simplify collection initialization
                List<INamedTypeSymbol> membersOfInterest = new();
#pragma warning restore IDE0028 // Simplify collection initialization

                foreach (INamedTypeSymbol member in typeMembers)
                {
                    ImmutableArray<AttributeData> attributes = member.GetAttributes();
                    
                    foreach (AttributeData attribute in attributes)
                    {
                        if (attribute.AttributeClass.Name == AttributeConstants.TargetAttributeName &&
                            attribute.AttributeClass.ToDisplayString() == AttributeConstants.TargetAttributeFullyQualifiedName)
                        {
                            membersOfInterest.Add(member);
                            break;
                        }
                    }
                }

                if (membersOfInterest.Count > 1)
                {
                    foreach (INamedTypeSymbol badMember in membersOfInterest)
                    {
                        ReportDiagnostic(ref namespaceContext, badMember);
                    }
                }
            }, SymbolKind.Namespace);
        }
        
        private static void ReportDiagnostic(ref SymbolAnalysisContext context, INamedTypeSymbol symbol)
        {
            Diagnostic diag = Diagnostic.Create(_descriptor, symbol.Locations[0], symbol.ContainingNamespace.ToDisplayString());
            context.ReportDiagnostic(diag);
        }
    }
}
