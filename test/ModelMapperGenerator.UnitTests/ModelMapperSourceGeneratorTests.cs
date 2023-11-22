using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModelMapperGenerator.Attributes;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ModelMapperGenerator.UnitTests
{
    public class ModelMapperSourceGeneratorTests
    {
        [Fact]
        public void SourceGenerator_GeneratesNothing_WhenNoValidInputIsPresent()
        {
            // Arrange
            string sourceText = """
                namespace Test;

                public class TestClass
                {
                    public void Hello()
                    {
                        
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeTest(sourceText);

            // Act
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertOutputCompilation(ref diagnostics, outputCompilation, 1);
        }

        [Fact]
        public void SourceGenerator_GeneratesNothing_WhenAttributeIsNotUsed()
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

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeTest(sourceText);

            // Act
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertOutputCompilation(ref diagnostics, outputCompilation, 1);
        }

        [Fact]
        public async Task SourceGenerator_GeneratesEnumModel_WhenAttributeIsUsed()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace Test
                {
                    [ModelGenerationTarget(FullyQualifiedTypes = new Type[] { typeof(Enums.Box) }) ]
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

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeTest(sourceText);

            // Act
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertOutputCompilation(ref diagnostics, outputCompilation, 3);
            string expectedModelString = """
                namespace Test;

                public enum BoxModel
                {
                    Big = 0,
                    Small = 1,
                }
                """;
            await AssertGeneratedCode("BoxModel.g.cs", expectedModelString, outputCompilation).ConfigureAwait(false);

            string expectedMapperString = """
                using System;
                using Enums;

                namespace Test;

                public static class BoxMapper
                {
                    public static BoxModel ToModel(this Box value)
                    {
                        return value switch
                        {
                            Box.Big => BoxModel.Big,
                            Box.Small => BoxModel.Small,
                            _ => throw new ArgumentOutOfRangeException("Unknown enum value.")
                        };
                    }

                    public static Box ToDomain(this BoxModel value)
                    {
                        return value switch
                        {
                            BoxModel.Big => Box.Big,
                            BoxModel.Small => Box.Small,
                            _ => throw new ArgumentOutOfRangeException("Unknown enum value.")
                        };
                    }
                }
                """;
            await AssertGeneratedCode("BoxMapper.g.cs", expectedMapperString, outputCompilation).ConfigureAwait(false);
        }

        private (Compilation, GeneratorDriver) ArrangeTest(string sourceText)
        {
            Compilation inputCompilation = CreateCompilation(sourceText);
            ModelMapperSourceGenerator testedGenerator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(testedGenerator);
            return (inputCompilation, driver);
        }

        private void AssertOutputCompilation(ref ImmutableArray<Diagnostic> diagnostics, Compilation outputCompilation, int expectedSyntaxTreeCount)
        {
            Assert.True(diagnostics.IsEmpty);
            Assert.True(outputCompilation.SyntaxTrees.Count() == expectedSyntaxTreeCount);
            Assert.True(outputCompilation.GetDiagnostics().IsEmpty);
        }

        private async Task AssertGeneratedCode(string expectedFileName, string expectedFileContent, Compilation outputCompilation)
        {
            SyntaxTree? syntaxTree = outputCompilation.SyntaxTrees.FirstOrDefault(x => x.FilePath.EndsWith(expectedFileName));
            Assert.NotNull(syntaxTree);

            SourceText sourceText = await syntaxTree.GetTextAsync().ConfigureAwait(false);
            string stringOfSourceText = sourceText.ToString();
            Assert.Equal(expectedFileContent, stringOfSourceText);
        }

        private Compilation CreateCompilation(string source)
        {
            SyntaxTree parsedSyntaxTree = CSharpSyntaxTree.ParseText(source);
            SyntaxTree[] syntaxTrees = [parsedSyntaxTree];
            Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            PortableExecutableReference binderReference = CreateReference(typeof(Binder));
            PortableExecutableReference attrReference = CreateReference(typeof(ModelGenerationTarget));
            PortableExecutableReference netstandard = CreateReference("netstandard", currentAssemblies);
            PortableExecutableReference runtime = CreateReference("System.Runtime", currentAssemblies);

            PortableExecutableReference[] references = [binderReference, attrReference, runtime, netstandard];
            CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);

            Compilation compilation = CSharpCompilation.Create("testCompilation", syntaxTrees, references, options);
            return compilation;
        }

        private PortableExecutableReference CreateReference(string assemblyName, Assembly[] assemblies)
        {
            Assembly? assembly = assemblies.FirstOrDefault(x => x.GetName().FullName.Contains(assemblyName));
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            PortableExecutableReference reference = MetadataReference.CreateFromFile(assembly.Location);
            return reference;
        }

        private PortableExecutableReference CreateReference(Type type)
        {
            string path = type.GetTypeInfo().Assembly.Location;
            PortableExecutableReference reference = MetadataReference.CreateFromFile(path);
            return reference;
        }
    } 
}
