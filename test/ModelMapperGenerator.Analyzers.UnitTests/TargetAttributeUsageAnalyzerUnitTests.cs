using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ModelMapperGenerator.Analyzer.UnitTests.Infrastructure;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzer.UnitTests
{
    public class TargetAttributeUsageAnalyzerUnitTests
    {
        [Fact]
        public async Task DoesNotReportDiagnostics_WhenSingleHookExists()
        {
            // Arrange
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.AddressType)
                })]
                public class FirstHook {}
            }

            namespace SourceNamespace
            {
                public enum AddressType
                {
                    Business,
                    Home
                }
            }
            """;
            TargetAttributeUsageAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, _) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);

            // Act
            ImmutableArray<Diagnostic> diags = await compilationWithAnalyzers.GetAllDiagnosticsAsync(default);

            // Assert
            Assert.Empty(diags);
        }

        [Fact]
        public async Task DoesNotReportDiagnostics_WhenMultipleHooksExist_InMultipleNamespaces()
        {
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace.First
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.AddressType)
                })]
                public class FirstHook {}
            }

            namespace TargetNamespace.Second
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.AddressType)
                })]
                public class SecondHook {}
            }

            namespace SourceNamespace
            {
                public enum AddressType
                {
                    Business,
                    Home
                }
            }
            """;
            TargetAttributeUsageAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, _) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);

            // Act
            ImmutableArray<Diagnostic> diags = await compilationWithAnalyzers.GetAllDiagnosticsAsync(default);

            // Assert
            Assert.Empty(diags);
        }

        [Fact]
        public async Task ReportsDiagnostics_WhenMultipleHooksExist_InSingleNamespace()
        {
            // Arrange
            string generationSource = """
            using System;
            using ModelMapperGenerator.Attributes;
                
            namespace TargetNamespace
            {
                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.AddressType)
                })]
                public class FirstHook {}

                [ModelGenerationTarget(Types = new Type[] {
                    typeof(SourceNamespace.AddressType)
                })]
                public class SecondHook {}
            }

            namespace SourceNamespace
            {
                public enum AddressType
                {
                    Business,
                    Home
                }
            }
            """;
            TargetAttributeUsageAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);
            DiagnosticsAssertionContext context = CreateAssertionContext(compilation, 2,
                [
                    "(9,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace TargetNamespace",
                    "(14,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace TargetNamespace"
                ]);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = AssertHelpers.AssertAnalysisResult<TargetAttributeUsageAnalyzer>(result);
            AssertHelpers.AssertGeneratedDiagnostics(ref diags, context);
        }


        public static TheoryData<string, string[]> DiagnosticsTestData()
        {
            return new TheoryData<string, string[]>
            {
                {
                    """
                    using System;
                    using ModelMapperGenerator.Attributes;
                
                    namespace FirstTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FirstHook {}

                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class SecondHook {}
                    }

                    namespace SecondTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class ThirdHook {}
            
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FourthHook {}
                    }

                    namespace ThirdTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FifthHook {}
                    }

                    namespace SourceNamespace
                    {
                        public enum AddressType
                        {
                            Business,
                            Home
                        }
                    }
                    """,
                    [
                        "(9,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace",
                        "(14,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace",
                        "(22,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace",
                        "(27,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace"
                    ]
                },
                {
                    """
                    using System;
                    using ModelMapperGenerator.Attributes;
                    
                    namespace FirstTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FirstHook {}
                    }

                    namespace FirstTargetNamespace.FirstSubNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class SecondHook {}

                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class ThirdHook {}
                    }
                    
                    namespace FirstTargetNamespace.FirstSubNamespace.InnerNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FourthHook {}

                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FifthHook {}

                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class SixthHook {}
                    }
                    
                    namespace SecondTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class SeventhHook {}

                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class EighthHook {}
                    }
                    
                    namespace SourceNamespace
                    {
                        public enum AddressType
                        {
                            Business,
                            Home
                        }
                    }
                    """,
                    [
                        "(17,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstSubNamespace",
                        "(22,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstSubNamespace",
                        "(30,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstSubNamespace.InnerNamespace",
                        "(35,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstSubNamespace.InnerNamespace",
                        "(40,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstSubNamespace.InnerNamespace",
                        "(48,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace",
                        "(53,18): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace",
                    ]
                },
                {
                    """
                    using System;
                    using ModelMapperGenerator.Attributes;

                    namespace FirstTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FirstHook {}

                        namespace FirstInnerNamespace
                        {
                            [ModelGenerationTarget(Types = new Type[] {
                                typeof(SourceNamespace.AddressType)
                            })]
                            public class SecondHook {}

                            [ModelGenerationTarget(Types = new Type[] {
                                typeof(SourceNamespace.AddressType)
                            })]
                            public class ThirdHook {}
                        }
                    }

                    namespace SecondTargetNamespace
                    {
                        [ModelGenerationTarget(Types = new Type[] {
                            typeof(SourceNamespace.AddressType)
                        })]
                        public class FourthHook {}
                    
                        namespace SecondInnerNamespace
                        {
                            [ModelGenerationTarget(Types = new Type[] {
                                typeof(SourceNamespace.AddressType)
                            })]
                            public class FifthHook {}
                    
                            [ModelGenerationTarget(Types = new Type[] {
                                typeof(SourceNamespace.AddressType)
                            })]
                            public class SixthHook {}
                        }
                    }

                    namespace SourceNamespace
                    {
                        public enum AddressType
                        {
                            Business,
                            Home
                        }
                    }
                    """,
                    [
                        "(16,22): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstInnerNamespace",
                        "(21,22): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace FirstTargetNamespace.FirstInnerNamespace",
                        "(37,22): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace.SecondInnerNamespace",
                        "(42,22): error MMG1: There should only be a single ModelGenerationTarget attribute usage in namespace SecondTargetNamespace.SecondInnerNamespace",
                    ]
                }
            };
        }

        [Theory]
        [MemberData(nameof(DiagnosticsTestData))]
        public async Task ReportsDiagnostics_WhenMultipleHooksExist_InMultipleSharedNamespaces(string generationSource, string[] expectedMessages)
        {
            // Arrange
            TargetAttributeUsageAnalyzer analyzer = new();
            (CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation) = ArrangeHelpers.ArrangeTest(generationSource, analyzer);
            DiagnosticsAssertionContext context = CreateAssertionContext(compilation, expectedMessages.Length, expectedMessages);

            // Act
            AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);

            // Assert
            ImmutableArray<Diagnostic> diags = AssertHelpers.AssertAnalysisResult<TargetAttributeUsageAnalyzer>(result);
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
                Id = "MMG1",
                DefaultSeverity = DiagnosticSeverity.Error,
                Severity = DiagnosticSeverity.Error,
                Category = "ModelMapperGenerator.ModelGenerationTarget",
                MessageFormat = "There should only be a single ModelGenerationTarget attribute usage in namespace {0}",
                Title = "Invalid generation targets",
                Description = "Applying ModelGenerationTarget attribute to more than one class in a namespace can cause incorrect behavior and will prevent the generator from working.",
            };

            return context;
        }
    }
}