using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzers.UnitTests.Infrastructure
{
    internal static class AssertHelpers
    {
        public static ImmutableArray<Diagnostic> AssertCompilationAnalyzerResult<TAnalyzer>(AnalysisResult result) where TAnalyzer : DiagnosticAnalyzer
        {
            Assert.Empty(result.AdditionalFileDiagnostics);
            Assert.Empty(result.SemanticDiagnostics);
            Assert.Empty(result.SyntaxDiagnostics);
            DiagnosticAnalyzer analyzer = Assert.Single(result.CompilationDiagnostics.Keys);
            Assert.IsType<TAnalyzer>(analyzer);
            ImmutableArray<Diagnostic> diagnosticsArray = result.CompilationDiagnostics[analyzer];
            return diagnosticsArray;
        }

        public static ImmutableArray<Diagnostic> AssertAnalysisResult<TAnalyzer>(AnalysisResult result) where TAnalyzer : DiagnosticAnalyzer
        {
            Assert.Empty(result.AdditionalFileDiagnostics);
            Assert.Empty(result.CompilationDiagnostics);
            Assert.Empty(result.SyntaxDiagnostics);
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> diags = Assert.Single(result.SemanticDiagnostics).Value;
            DiagnosticAnalyzer analyzer = Assert.Single(diags.Keys);
            Assert.IsType<TAnalyzer>(analyzer);
            ImmutableArray<Diagnostic> diagnosticsArray = diags[analyzer];
            return diagnosticsArray;
        }

        public static void AssertGeneratedDiagnostics(ref ImmutableArray<Diagnostic> diagnostics, DiagnosticsAssertionContext context, bool compilationDiagnostics = false)
        {
            Assert.Equal(context.ExpectedDiagnosticsCount, diagnostics.Length);
            SyntaxTree outputCompilationSyntaxTree = Assert.Single(context.Compilation.SyntaxTrees);
            Assert.All(diagnostics, (x) =>
            {
                Assert.Equal(context.WarningLevel, x.WarningLevel);
                Assert.Equal(context.Id, x.Id);
                Assert.Equal(context.DefaultSeverity, x.DefaultSeverity);
                Assert.Equal(context.Severity, x.Severity);
                Assert.False(x.IsSuppressed);
                Assert.False(x.IsWarningAsError);

                DiagnosticDescriptor descriptor = x.Descriptor;
                Assert.Equal(context.Category, descriptor.Category);
                Assert.Equal(context.DefaultSeverity, descriptor.DefaultSeverity);
                Assert.Equal(context.Id, descriptor.Id);
                Assert.Equal(context.MessageFormat, descriptor.MessageFormat);
                Assert.Equal(context.Title, descriptor.Title);
                Assert.Equal(context.Description, descriptor.Description);

                if (compilationDiagnostics)
                {
                    string tag = Assert.Single(descriptor.CustomTags);
                    Assert.Equal(WellKnownDiagnosticTags.CompilationEnd, tag);
                }
                else
                {
                    Assert.Empty(descriptor.CustomTags);
                }
                Assert.True(descriptor.IsEnabledByDefault);

                Location location = x.Location;
                Assert.True(location.IsInSource);
                Assert.Equal(outputCompilationSyntaxTree, location.SourceTree);
            });

            foreach (var expectedMessage in context.ExpectedMessages)
            {
                Assert.Contains(diagnostics, x =>
                {
                    string message = x.ToString();
                    bool messageContained = context.ExpectedMessages.Contains(message);
                    return messageContained;
                });
            }
        }
    }
}
