using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Analyzer.UnitTests.Infrastructure;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzers.UnitTests
{
    public class TypesWithSameNameNotAllowedAnalyzerUnitTests
    {
        [Fact]
        public async Task ReportsDiagnostics_WhenTypesWithTheSameNameArePlacedOnTheSameTargetAttribute()
        {
            // Arrange
            string source = """
                using System;
                using ModelMapperGenerator.Attributes;
                using ThirdSourceNamespace;    

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(FirstSourceNamespace.Address),
                        typeof(SecondSourceNamespace.Address),
                        typeof(BoxType)
                    })]
                    public class FirstHook {}
                }
                
                namespace FirstSourceNamespace
                {
                    public enum AddressType
                    {
                        Home,
                        Business
                    }
                }
                
                namespace SecondSourceNamespace
                {
                    public enum AddressType
                    {
                        Home,
                        Business
                    }
                }

                namespace ThirdSourceNamespace
                {
                    public enum BoxType
                    {
                        Big,
                        Small
                    }
                }
                """;

            TypesWithSameNameNotAllowedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(source, analyzer);
            string[] expectedMessages = [
                "(8,9): error MMG3: Multiple types with name Address have been placed on the same ModelGenerationTargetAttribute, which is not supported",
                "(9,9): error MMG3: Multiple types with name Address have been placed on the same ModelGenerationTargetAttribute, which is not supported"
            ];
            DiagnosticsAssertionContext context = CreateAssertionContext(compilation, 2, expectedMessages);
            
            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = AssertHelpers.AssertAnalysisResult<TypesWithSameNameNotAllowedAnalyzer>(result);
            AssertHelpers.AssertGeneratedDiagnostics(ref diags, context);
        }

        [Fact]
        public async Task DoesNotReportDiagnostics_WhenAllTypesHaveDifferentNames()
        {
            // Arrange
            string source = """
                using System;
                using ModelMapperGenerator.Attributes;
                using FirstSourceNamespace;    

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(Address),
                        typeof(BoxType)
                    })]
                    public class FirstHook {}
                }
                
                namespace FirstSourceNamespace
                {
                    public enum AddressType
                    {
                        Home,
                        Business
                    }

                    public enum BoxType
                    {
                        Big,
                        Small
                    }
                }
                """;
            TypesWithSameNameNotAllowedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(source, analyzer);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = result.GetAllDiagnostics();
            Assert.Empty(diags);
        }

        [Fact]
        public async Task DoesNotReportDiagnostics_WhenTypesWithTheSameNameArePlacedOnDifferentTargets()
        {
            // Arrange
            string source = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace FirstTargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(FirstSourceNamespace.Address),
                    })]
                    public class FirstHook {}
                }

                namespace SecondTargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SecondSourceNamespace.Address),
                    })]
                    public class SecondHook {}
                }
                
                namespace FirstSourceNamespace
                {
                    public enum AddressType
                    {
                        Home,
                        Business
                    }

                }

                namespace SecondSourceNamespace
                {
                    public enum AddressType
                    {
                        Home,
                        Business
                    }
                }
                """;
            TypesWithSameNameNotAllowedAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(source, analyzer);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = result.GetAllDiagnostics();
            Assert.Empty(diags);
        }

        private static DiagnosticsAssertionContext CreateAssertionContext(Compilation compilation, int expectedDiagnosticsCount, string[] expectedMessages)
        {
            DiagnosticsAssertionContext context = new()
            {
                Compilation = compilation,
                ExpectedDiagnosticsCount = expectedDiagnosticsCount,
                ExpectedMessages = expectedMessages,
                WarningLevel = 0,
                Id = "MMG3",
                DefaultSeverity = DiagnosticSeverity.Error,
                Severity = DiagnosticSeverity.Error,
                Category = "ModelMapperGenerator.ModelGenerationTarget",
                MessageFormat = "Multiple types with name {0} have been placed on the same ModelGenerationTargetAttribute, which is not supported",
                Title = "Multiple types with the same name are not supported",
                Description = "Placing multiple types with the same name on the same ModelGenerationTargetAttribute is not supported, separate these types into targets placed in separate namespaces.",
            };

            return context;
        }
    }
}
