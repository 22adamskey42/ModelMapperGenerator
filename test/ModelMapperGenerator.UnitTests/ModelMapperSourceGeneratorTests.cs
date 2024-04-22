using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModelMapperGenerator.TestInfrastructure;
using ModelMapperGenerator.UnitTests.Infrastructure;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace ModelMapperGenerator.UnitTests
{
    public class ModelMapperSourceGeneratorTests
    {
        public static TheoryData<string> SyntaxTestParams()
        {
            string implicitlyTypedArray = """
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new[] {
                        typeof(SourceNamespace.AddressType)
                    })]
                    public class Hook {}
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

            string explicitlyTypedArray = """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.AddressType)
                    })]
                    public class Hook {}
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

            string collectionExpression = """
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = [
                        typeof(SourceNamespace.AddressType)
                    ])]
                    public class Hook { }
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

            return new TheoryData<string>(implicitlyTypedArray, explicitlyTypedArray, collectionExpression);
        }

        [Theory]
        [MemberData(nameof(SyntaxTestParams))]
        public async Task SourceGenerator_SupportsDifferentArraySyntax(string inputSource)
        {
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(inputSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);
            string addressTypeModel = """
                namespace TargetNamespace
                {
                    public enum AddressTypeModel
                    {
                        Business = 0,
                        Home = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressTypeModel.g.cs", addressTypeModel, outputCompilation);

            string addressTypeMapper = """
                using System;
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class AddressTypeMapper
                    {
                        public static AddressTypeModel ToModel(this AddressType value)
                        {
                            return value switch
                            {
                                AddressType.Business => AddressTypeModel.Business,
                                AddressType.Home => AddressTypeModel.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static AddressType ToDomain(this AddressTypeModel value)
                        {
                            return value switch
                            {
                                AddressTypeModel.Business => AddressType.Business,
                                AddressTypeModel.Home => AddressType.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressTypeMapper.g.cs", addressTypeMapper, outputCompilation);

        }

        [Fact]
        public void SourceGenerator_CachesGenerationSteps_ForEnum()
        {
            string generationSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.AddressType)
                    })]
                    public class Hook {}
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
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);
            

            // Act
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation firstOutputCompilation, out ImmutableArray<Diagnostic> _);
            GeneratorDriverRunResult firstRunResult = driver.GetRunResult();

            driver = driver.RunGeneratorsAndUpdateCompilation(firstOutputCompilation, out Compilation _, out ImmutableArray<Diagnostic> _);
            GeneratorDriverRunResult secondRunResult = driver.GetRunResult();

            // Assert
            AssertHelpers.AssertGeneratorRunResultsCaching(firstRunResult, secondRunResult);
        }

        [Fact]
        public void SourceGenerator_CachesGenerationSteps_ForClass()
        {
            string generationSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.Person)
                    })]
                    public class Hook {}
                }

                namespace SourceNamespace
                {
                    public class Person
                    {
                        public string FirstName { get; set; }
                        public int Age { get; set; }
                    }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation firstOutputCompilation, out ImmutableArray<Diagnostic> _);
            GeneratorDriverRunResult firstRunResult = driver.GetRunResult();

            driver = driver.RunGeneratorsAndUpdateCompilation(firstOutputCompilation, out Compilation _, out ImmutableArray<Diagnostic> _);
            GeneratorDriverRunResult secondRunResult = driver.GetRunResult();

            // Assert
            AssertHelpers.AssertGeneratorRunResultsCaching(firstRunResult, secondRunResult);
        }

        [Fact]
        public async Task GeneratesMultipleModelsAndMappers_WhenHooksAreInDifferentNamespacesAsync()
        {
            // Arrange
            string generationSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace FirstTargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.AddressType)
                    })]
                    public class FirstHook {}
                }

                namespace SecondTargetNamespace
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

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 5);

            string firstAddressTypeModel = """
                namespace FirstTargetNamespace
                {
                    public enum AddressTypeModel
                    {
                        Business = 0,
                        Home = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("FirstTargetNamespace.SourceNamespace.AddressTypeModel.g.cs", firstAddressTypeModel, outputCompilation);

            string firstAddressTypeMapper = """
                using System;
                using SourceNamespace;
                
                namespace FirstTargetNamespace
                {
                    public static class AddressTypeMapper
                    {
                        public static AddressTypeModel ToModel(this AddressType value)
                        {
                            return value switch
                            {
                                AddressType.Business => AddressTypeModel.Business,
                                AddressType.Home => AddressTypeModel.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static AddressType ToDomain(this AddressTypeModel value)
                        {
                            return value switch
                            {
                                AddressTypeModel.Business => AddressType.Business,
                                AddressTypeModel.Home => AddressType.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("FirstTargetNamespace.SourceNamespace.AddressTypeMapper.g.cs", firstAddressTypeMapper, outputCompilation);

            string secondAddressTypeModel = """
                namespace SecondTargetNamespace
                {
                    public enum AddressTypeModel
                    {
                        Business = 0,
                        Home = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("SecondTargetNamespace.SourceNamespace.AddressTypeModel.g.cs", secondAddressTypeModel, outputCompilation);

            string secondAddressTypeMapper = """
                using System;
                using SourceNamespace;
                
                namespace SecondTargetNamespace
                {
                    public static class AddressTypeMapper
                    {
                        public static AddressTypeModel ToModel(this AddressType value)
                        {
                            return value switch
                            {
                                AddressType.Business => AddressTypeModel.Business,
                                AddressType.Home => AddressTypeModel.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static AddressType ToDomain(this AddressTypeModel value)
                        {
                            return value switch
                            {
                                AddressTypeModel.Business => AddressType.Business,
                                AddressTypeModel.Home => AddressType.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("SecondTargetNamespace.SourceNamespace.AddressTypeMapper.g.cs", secondAddressTypeMapper, outputCompilation);
        }

        [Fact]
        public async Task SourceGenerator_GeneratesSource_WithoutFullyQualifiedTypes_ForClassAsync()
        {
            string generationSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                using SourceNamespace;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(Person)
                    })]
                    public class Hook {}
                }

                namespace SourceNamespace
                {
                    public class Person
                    {
                        public string FirstName { get; set; }
                        public int Age { get; set; }
                    }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);

            string personModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public int Age { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonModel.g.cs", personModel, outputCompilation);

            string personMapper = """
                using SourceNamespace;
                
                namespace TargetNamespace
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                FirstName = value.FirstName,
                                Age = value.Age,
                
                            };
                
                            return model;
                        }
                
                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                FirstName = value.FirstName,
                                Age = value.Age,
                
                            };
                
                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonMapper.g.cs", personMapper, outputCompilation);
        }

        [Fact]
        public async Task SourceGenerator_GeneratesSource_WithoutFullyQualifiedTypes_ForEnumAsync()
        {
            string generationSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                using SourceNamespace;
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(AddressType)
                    })]
                    public class Hook {}
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
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);

            string addressTypeModel = """
                namespace TargetNamespace
                {
                    public enum AddressTypeModel
                    {
                        Business = 0,
                        Home = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressTypeModel.g.cs", addressTypeModel, outputCompilation);

            string addressTypeMapper = """
                using System;
                using SourceNamespace;
                
                namespace TargetNamespace
                {
                    public static class AddressTypeMapper
                    {
                        public static AddressTypeModel ToModel(this AddressType value)
                        {
                            return value switch
                            {
                                AddressType.Business => AddressTypeModel.Business,
                                AddressType.Home => AddressTypeModel.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static AddressType ToDomain(this AddressTypeModel value)
                        {
                            return value switch
                            {
                                AddressTypeModel.Business => AddressType.Business,
                                AddressTypeModel.Home => AddressType.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressTypeMapper.g.cs", addressTypeMapper, outputCompilation);
        }

        [Fact]
        public async void SourceGenerator_GeneratesSourcesForTypesInReferencedAssemblies()
        {
            // Arrange
            string externalSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                
                namespace ExternalSource
                {
                    public enum AddressType
                    {
                        Business,
                        Home
                    }
                }
                """;

            string localSource = """
                using System;
                using ModelMapperGenerator.Attributes;
                using ExternalSource;
                using LocalSource;
                
                namespace LocalSource
                {
                    public enum BoxType
                    {
                        Big,
                        Small
                    }
                }

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(AddressType),
                        typeof(BoxType),
                    })]
                    public class Hook {}
                }
                """;

            Compilation externalCompilation = CompilationBuilder.CreateCompilation(externalSource);
            Compilation localCompilation = CompilationBuilder.CreateCompilation(localSource);
            CompilationReference meta = externalCompilation.ToMetadataReference();

            Compilation updatedSecondCompilation = localCompilation.AddReferences(meta);

            ImmutableArray<Diagnostic> diags = updatedSecondCompilation.GetDiagnostics();
            ModelMapperSourceGenerator testedGenerator = new();
            ISourceGenerator sourceGenerator = testedGenerator.AsSourceGenerator();
            GeneratorDriverOptions opts = new(IncrementalGeneratorOutputKind.None, true);
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: opts);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(updatedSecondCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diag);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diag, outputCompilation, 5);

            string expectedAddressTypeModel = """
                namespace TargetNamespace
                {
                    public enum AddressTypeModel
                    {
                        Business = 0,
                        Home = 1,
                
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ExternalSource.AddressTypeModel.g.cs", expectedAddressTypeModel, outputCompilation);

            string expectedAddressTypeMapper = """
                using System;
                using ExternalSource;
                
                namespace TargetNamespace
                {
                    public static class AddressTypeMapper
                    {
                        public static AddressTypeModel ToModel(this AddressType value)
                        {
                            return value switch
                            {
                                AddressType.Business => AddressTypeModel.Business,
                                AddressType.Home => AddressTypeModel.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static AddressType ToDomain(this AddressTypeModel value)
                        {
                            return value switch
                            {
                                AddressTypeModel.Business => AddressType.Business,
                                AddressTypeModel.Home => AddressType.Home,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ExternalSource.AddressTypeMapper.g.cs", expectedAddressTypeMapper, outputCompilation);

            string expectedBoxTypeModel = """
                namespace TargetNamespace
                {
                    public enum BoxTypeModel
                    {
                        Big = 0,
                        Small = 1,
                
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.LocalSource.BoxTypeModel.g.cs", expectedBoxTypeModel, outputCompilation);

            string expectedBoxTypeMapper = """
                using System;
                using LocalSource;
                
                namespace TargetNamespace
                {
                    public static class BoxTypeMapper
                    {
                        public static BoxTypeModel ToModel(this BoxType value)
                        {
                            return value switch
                            {
                                BoxType.Big => BoxTypeModel.Big,
                                BoxType.Small => BoxTypeModel.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static BoxType ToDomain(this BoxTypeModel value)
                        {
                            return value switch
                            {
                                BoxTypeModel.Big => BoxType.Big,
                                BoxTypeModel.Small => BoxType.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.LocalSource.BoxTypeMapper.g.cs", expectedBoxTypeMapper, outputCompilation);

        }

        [Fact]
        public async Task SourceGenerator_GeneratesSingleModelAndMapper_ForDuplicateTypesInTargetAttribute()
        {
            // Arrange
            string source = """
                using System;
                using LocalSource;
                using ModelMapperGenerator.Attributes;

                namespace LocalSource
                {
                    public enum BoxType
                    {
                        Big,
                        Small
                    }
                }

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(BoxType),
                        typeof(BoxType),
                    })]
                    public class Hook {}
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(source);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);
            string expectedBoxTypeModel = """
                namespace TargetNamespace
                {
                    public enum BoxTypeModel
                    {
                        Big = 0,
                        Small = 1,
                
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.LocalSource.BoxTypeModel.g.cs", expectedBoxTypeModel, outputCompilation);

            string expectedBoxTypeMapper = """
                using System;
                using LocalSource;
                
                namespace TargetNamespace
                {
                    public static class BoxTypeMapper
                    {
                        public static BoxTypeModel ToModel(this BoxType value)
                        {
                            return value switch
                            {
                                BoxType.Big => BoxTypeModel.Big,
                                BoxType.Small => BoxTypeModel.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static BoxType ToDomain(this BoxTypeModel value)
                        {
                            return value switch
                            {
                                BoxTypeModel.Big => BoxType.Big,
                                BoxTypeModel.Small => BoxType.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.LocalSource.BoxTypeMapper.g.cs", expectedBoxTypeMapper, outputCompilation);

        }
    } 
}
