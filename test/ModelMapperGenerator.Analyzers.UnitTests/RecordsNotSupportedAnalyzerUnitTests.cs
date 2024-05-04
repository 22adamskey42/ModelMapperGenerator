using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using ModelMapperGenerator.Analyzers.UnitTests.Infrastructure;

namespace ModelMapperGenerator.Analyzers.UnitTests
{
    public class RecordsNotSupportedAnalyzerUnitTests
    {
        public static TheoryData<string> EmptyDiagnosticsData()
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            return new TheoryData<string>
            {
                """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.Address)
                    })]
                    public class FirstHook {}
                }

                namespace SourceNamespace
                {
                    [ModelGenerationSource]
                    public class Address
                    {
                        public string Street { get; set; }
                    }
                }
                """,
                """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.Address)
                    })]
                    public class FirstHook {}
                }

                namespace SourceNamespace
                {
                    [ModelGenerationSource]
                    public enum Address
                    {
                        Home,
                        Business
                    }
                }
                """,
            };
#pragma warning restore IDE0028 // Simplify collection initialization
        }

        [Theory]
        [MemberData(nameof(EmptyDiagnosticsData))]
        public async Task DoesNotReportsDiagnostics_WhenSourceTypeIsNotARecord(string generationSource)
        {
            // Arrange
            RecordsNotSupportedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, _) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = result.GetAllDiagnostics();
            Assert.Empty(diags);
        }

        [Fact]
        public async Task ReportsDiagnostics_WhenSourceTypeIsARecord()
        {
            // Arrange
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.Address)
                })]
                public class FirstHook {}
            }

            namespace SourceNamespace
            {
                public record Address(string Street, int Number);
            }
            """;
            RecordsNotSupportedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);
            DiagnosticsAssertionContext context = CreateAssertionContext(compilation, 1,
                [
                    "(7,9): warning MMG2: Type SourceNamespace.Address used in ModelGenerationTargetAttribute is a record, which is not supported",
                ]);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = AssertHelpers.AssertAnalysisResult<RecordsNotSupportedAnalyzer>(result);
            AssertHelpers.AssertGeneratedDiagnostics(ref diags, context);
        }

        private static DiagnosticsAssertionContext CreateAssertionContext(Compilation compilation, int expectedDiagnosticsCount, string[] expectedMessages)
        {
            DiagnosticsAssertionContext context = new()
            {
                Compilation = compilation,
                ExpectedDiagnosticsCount = expectedDiagnosticsCount,
                ExpectedMessages = expectedMessages,
                WarningLevel = 1,
                Id = "MMG2",
                DefaultSeverity = DiagnosticSeverity.Warning,
                Severity = DiagnosticSeverity.Warning,
                Category = "ModelMapperGenerator.ModelGenerationSource",
                MessageFormat = "Type {0} used in ModelGenerationTargetAttribute is a record, which is not supported",
                Title = "Records are not supported",
                Description = "Record types are not supported and will be ignored by the source generator.",
            };

            return context;
        }
    }
}
