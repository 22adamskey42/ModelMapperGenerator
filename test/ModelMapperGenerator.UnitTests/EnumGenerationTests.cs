using Microsoft.CodeAnalysis;
using ModelMapperGenerator.UnitTests.Infrastructure;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace ModelMapperGenerator.UnitTests
{
    public class EnumGenerationTests
    {
        [Fact]
        public void GeneratesNothing_WhenTypeHasNoMembers_Enum()
        {
            // Arrange
            string sourceText = """
                using ModelMapperGenerator.Attributes;
                using System;

                namespace TargetNamespace 
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(SourceNamespace.Box) }) ]
                    public class Hook { }
                }

                namespace SourceNamespace
                {

                    public enum Box { }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 1);
        }

        [Fact]
        public void GeneratesNothing_WhenTargetAttributeIsNotUsed_Enum()
        {
            // Arrange
            string sourceText = """
                namespace Test
                {
                    public class TestClass {}
                }

                namespace Enums 
                {
                    public enum Box 
                    {
                        Big,
                        Small
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 1);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_WhenSourceAndTargetAttributesAreUsed_Enum()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(Enums.Box) }) ]
                    public class TestClass {}
                }

                namespace Enums 
                {
                    public enum Box 
                    {
                        Big,
                        Small
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);
            string expectedModelString = """
                namespace Test
                {
                    public enum BoxModel
                    {
                        Big = 0,
                        Small = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.BoxModel.g.cs", expectedModelString, outputCompilation);

            string expectedMapperString = """
                using System;
                using Enums;

                namespace Test
                {
                    public static class BoxMapper
                    {
                        public static BoxModel ToModel(this Box value)
                        {
                            return value switch
                            {
                                Box.Big => BoxModel.Big,
                                Box.Small => BoxModel.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static Box ToDomain(this BoxModel value)
                        {
                            return value switch
                            {
                                BoxModel.Big => Box.Big,
                                BoxModel.Small => Box.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.BoxMapper.g.cs", expectedMapperString, outputCompilation);
        }
    }
}
