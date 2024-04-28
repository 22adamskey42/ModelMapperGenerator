using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Analyzers.UnitTests.Infrastructure;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzers.UnitTests
{
    public class OpenGenericsNotSupportedAnalyzerUnitTests
    {
        [Fact]
        public async Task DoesNotReportDiagnostic_WhenTypeIsNotOpenGeneric()
        {
            // Arrange
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.Person<int>)
                })]
                public class FirstHook {}
            }

            namespace SourceNamespace
            {
                public class Person<T>
                {
                    public string Name {get; set;}
                    public T Grade {get; set;}
                }
            }
            """;
            OpenGenericsNotSupportedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, _) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = result.GetAllDiagnostics();
            Assert.Empty(diags);
        }

        [Fact]
        public async Task ReportsDiagnostics_WhenTypeIsAnOpenGeneric()
        {
            // Arrange
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.Person<>)
                })]
                public class FirstHook {}
            }

            namespace SourceNamespace
            {
                public class Person<T>
                {
                    public string Name {get; set;}
                    public T Grade {get; set;}
                }
            }
            """;
            OpenGenericsNotSupportedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);
            DiagnosticsAssertionContext context = CreateAssertionContext(compilation, 1,
                [
                    "(7,9): error MMG4: Typeof expression used in ModelGenerationTargetAttribute is an open generic, which is not supported",
                ]);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = AssertHelpers.AssertAnalysisResult<OpenGenericsNotSupportedAnalyzer>(result);
            AssertHelpers.AssertGeneratedDiagnostics(ref diags, context);
        }

        private static DiagnosticsAssertionContext CreateAssertionContext(Compilation compilation, int expectedDiagnosticsCount, string[] expectedMessages)
        {
            DiagnosticsAssertionContext context = new()
            {
                Compilation = compilation,
                ExpectedDiagnosticsCount = expectedDiagnosticsCount,
                ExpectedMessages = expectedMessages,
                WarningLevel = 0,
                Id = "MMG4",
                DefaultSeverity = DiagnosticSeverity.Error,
                Severity = DiagnosticSeverity.Error,
                Category = "ModelMapperGenerator.ModelGenerationTarget",
                MessageFormat = "Typeof expression used in ModelGenerationTargetAttribute is an open generic, which is not supported",
                Title = "Open generics are not supported",
                Description = "Open generics are not supported by the source generator.",
            };

            return context;
        }
    }
}
