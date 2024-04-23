using Microsoft.CodeAnalysis;
using ModelMapperGenerator.UnitTests.Infrastructure;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace ModelMapperGenerator.UnitTests
{
    public class ClassGenerationTests
    {
        public static TheoryData<string, bool> EmptyModelTestData()
        {
            return new TheoryData<string, bool>
            {
                {
                    string.Empty,
                    true
                },
                {
                    "public Box() { }",
                    true
                },
                {
                    "private string _field;",
                    false
                },
                {
                    "internal string _field;",
                    false
                },
                {
                    "protected string _field;",
                    false
                },
                {
                    "protected internal string _field;",
                    false
                },
                {
                    "private protected string _field;",
                    false
                },
                {
                    "private string Property { get; set; }",
                    true
                },
                {
                    "internal string Property { get; set; }",
                    true
                },
                {
                    "protected string Property { get; set; }",
                    true
                },
                {
                    "protected internal string Property { get; set; }",
                    true
                },
                {
                    "private protected string Property { get; set; }",
                    true
                }
            };
        }

        [Theory]
        [MemberData(nameof(EmptyModelTestData))]
        public async Task GeneratesEmptyModelAndMapper_ClassAsync(string setup, bool verifyDiagsEmpty)
        {
            // Arrange
            string sourceText = $$"""
                using ModelMapperGenerator.Attributes;
                using System;
                
                namespace TargetNamespace 
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(SourceNamespace.Box) }) ]
                    public class Hook { }
                }
                
                namespace SourceNamespace
                {
                    public class Box 
                    {
                        {{setup}}
                    }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3, verifyDiagsEmpty);
            string expectedModelString = """
                namespace TargetNamespace
                {
                    public class BoxModel
                    {
                
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxModel.g.cs", expectedModelString, outputCompilation);

            string expectedMapperString = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class BoxMapper
                    {
                        public static BoxModel ToModel(this Box value)
                        {
                            BoxModel model = new BoxModel()
                            {

                            };

                            return model;
                        }

                        public static Box ToDomain(this BoxModel value)
                        {
                            Box domain = new Box()
                            {

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxMapper.g.cs", expectedMapperString, outputCompilation);
        }

        [Fact]
        public async Task DoesNotGenerateSetterInMapper_WhenSourceClassDoesNotHaveAPublicSetter()
        {
            // Arrange
            string sourceText = $$"""
                using ModelMapperGenerator.Attributes;
                using System;
                
                namespace TargetNamespace 
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(SourceNamespace.Box) }) ]
                    public class Hook { }
                }
                
                namespace SourceNamespace
                {
                    public class Box 
                    {
                        public string Property { get; private set; }
                    }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 3);
            string expectedModelString = """
                namespace TargetNamespace
                {
                    public class BoxModel
                    {
                        public string Property { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxModel.g.cs", expectedModelString, outputCompilation);

            string expectedMapperString = """
                using SourceNamespace;
                
                namespace TargetNamespace
                {
                    public static class BoxMapper
                    {
                        public static BoxModel ToModel(this Box value)
                        {
                            BoxModel model = new BoxModel()
                            {
                                Property = value.Property,

                            };
                
                            return model;
                        }
                
                        public static Box ToDomain(this BoxModel value)
                        {
                            Box domain = new Box()
                            {
                
                            };
                
                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxMapper.g.cs", expectedMapperString, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_TargetAttributesIsUsedAsync_Class()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(Classes.Person) }) ]
                    public class TestClass {}
                }

                namespace Classes 
                {
                    public class Person
                    {
                        public string FirstName { get; set; }
                        public int Age { get; set; }
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
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public int Age { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonModel.g.cs", expectedModelString, outputCompilation);

            string expectedMapperString = """
                using Classes;

                namespace Test
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
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonMapper.g.cs", expectedMapperString, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_FromComplexClasses_SameNamespace_ClassAsync()
        {
            // Arrange
            string sourceText = """
                using ModelMapperGenerator.Attributes;
                using System;

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.AddressType),
                        typeof(SourceNamespace.Address),
                        typeof(SourceNamespace.PersonType),
                        typeof(SourceNamespace.Person)
                    })]
                    public class Hook
                    {

                    }
                }

                namespace SourceNamespace
                {
                    public enum AddressType
                    {
                        Business,
                        Home
                    }

                    public class Address
                    {
                        public string City { get; set; }
                        public Guid AddressId { get; set; }
                        public AddressType AddressType { get; set; }
                    }

                    public enum PersonType
                    {
                        Employee,
                        Customer
                    }

                    public class Person
                    {
                        public string FirstName { get; set; }
                        public PersonType PersonType { get; set; }
                        public Address Address { get; set; }
                    }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 9);
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

            string addressModel = """
                namespace TargetNamespace
                {
                    public class AddressModel
                    {
                        public string City { get; set; }
                        public System.Guid AddressId { get; set; }
                        public TargetNamespace.AddressTypeModel AddressType { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressModel.g.cs", addressModel, outputCompilation);

            string addressMapper = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class AddressMapper
                    {
                        public static AddressModel ToModel(this Address value)
                        {
                            AddressModel model = new AddressModel()
                            {
                                City = value.City,
                                AddressId = value.AddressId,
                                AddressType = value.AddressType.ToModel(),

                            };

                            return model;
                        }

                        public static Address ToDomain(this AddressModel value)
                        {
                            Address domain = new Address()
                            {
                                City = value.City,
                                AddressId = value.AddressId,
                                AddressType = value.AddressType.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.AddressMapper.g.cs", addressMapper, outputCompilation);

            string personTypeModel = """
                namespace TargetNamespace
                {
                    public enum PersonTypeModel
                    {
                        Employee = 0,
                        Customer = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonTypeModel.g.cs", personTypeModel, outputCompilation);

            string personTypeMapper = """
                using System;
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class PersonTypeMapper
                    {
                        public static PersonTypeModel ToModel(this PersonType value)
                        {
                            return value switch
                            {
                                PersonType.Employee => PersonTypeModel.Employee,
                                PersonType.Customer => PersonTypeModel.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static PersonType ToDomain(this PersonTypeModel value)
                        {
                            return value switch
                            {
                                PersonTypeModel.Employee => PersonType.Employee,
                                PersonTypeModel.Customer => PersonType.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonTypeMapper.g.cs", personTypeMapper, outputCompilation);

            string personModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public TargetNamespace.PersonTypeModel PersonType { get; set; }
                        public TargetNamespace.AddressModel Address { get; set; }

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
                                PersonType = value.PersonType.ToModel(),
                                Address = value.Address?.ToModel(),

                            };

                            return model;
                        }

                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                FirstName = value.FirstName,
                                PersonType = value.PersonType.ToDomain(),
                                Address = value.Address?.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonMapper.g.cs", personMapper, outputCompilation);
        }

        [Fact]
        public async Task GeneratesCorrectMapperAndModel_ForComplexClass_WhenNotAllPropertiesAreGenerationSources()
        {
            string sourceText = """
                using ModelMapperGenerator.Attributes;
                using System;

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                        typeof(SourceNamespace.PersonType),
                        typeof(SourceNamespace.Person),
                        typeof(SourceNamespace.Organization)
                    })]
                    public class Hook
                    {

                    }
                }

                namespace SourceNamespace
                {
                    public class Address
                    {
                        public string City { get; set; }
                        public int Number { get; set; }
                    }

                    public enum PersonStatus
                    {
                        Active,
                        Inactive
                    }

                    public class Organization
                    {
                        public string Name { get; set; }
                    }

                    public enum PersonType
                    {
                        Employee,
                        Customer
                    }

                    public class Person
                    {
                        public string FirstName { get; set; }
                        public PersonType PersonType { get; set; }
                        public Organization Organization { get; set; }
                        public Address Address { get; set; }
                        public PersonStatus PersonStatus { get; set; }
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 7);
            
            string organizationModel = """
                namespace TargetNamespace
                {
                    public class OrganizationModel
                    {
                        public string Name { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.OrganizationModel.g.cs", organizationModel, outputCompilation);

            string organizationMapper = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class OrganizationMapper
                    {
                        public static OrganizationModel ToModel(this Organization value)
                        {
                            OrganizationModel model = new OrganizationModel()
                            {
                                Name = value.Name,

                            };

                            return model;
                        }

                        public static Organization ToDomain(this OrganizationModel value)
                        {
                            Organization domain = new Organization()
                            {
                                Name = value.Name,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.OrganizationMapper.g.cs", organizationMapper, outputCompilation);

            string personTypeModel = """
                namespace TargetNamespace
                {
                    public enum PersonTypeModel
                    {
                        Employee = 0,
                        Customer = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonTypeModel.g.cs", personTypeModel, outputCompilation);

            string personTypeMapper = """
                using System;
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class PersonTypeMapper
                    {
                        public static PersonTypeModel ToModel(this PersonType value)
                        {
                            return value switch
                            {
                                PersonType.Employee => PersonTypeModel.Employee,
                                PersonType.Customer => PersonTypeModel.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static PersonType ToDomain(this PersonTypeModel value)
                        {
                            return value switch
                            {
                                PersonTypeModel.Employee => PersonType.Employee,
                                PersonTypeModel.Customer => PersonType.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonTypeMapper.g.cs", personTypeMapper, outputCompilation);

            string personModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public TargetNamespace.PersonTypeModel PersonType { get; set; }
                        public TargetNamespace.OrganizationModel Organization { get; set; }
                        public SourceNamespace.Address Address { get; set; }
                        public SourceNamespace.PersonStatus PersonStatus { get; set; }

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
                                PersonType = value.PersonType.ToModel(),
                                Organization = value.Organization?.ToModel(),
                                Address = value.Address,
                                PersonStatus = value.PersonStatus,

                            };

                            return model;
                        }

                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                FirstName = value.FirstName,
                                PersonType = value.PersonType.ToDomain(),
                                Organization = value.Organization?.ToDomain(),
                                Address = value.Address,
                                PersonStatus = value.PersonStatus,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonMapper.g.cs", personMapper, outputCompilation);
        }

        [Fact]
        public async Task GeneratesCorrectMapperAndModel_ForClass_WithPropertiesFromDifferentNamespaces()
        {
            string sourceText = """
                using ModelMapperGenerator.Attributes;
                using System;

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[] {
                                        typeof(FirstNamespace.Address),
                                        typeof(FirstNamespace.SubNamespace.Organization),
                                        typeof(SecondNamespace.PersonStatus),
                                        typeof(ThirdNamespace.Person),
                                        typeof(ThirdNamespace.SubThirdNamespace.PersonType)
                                    })]
                    public class Hook
                    {

                    }
                }

                namespace FirstNamespace
                {
                    public class Address
                    {
                        public string City { get; set; }
                        public int Number { get; set; }
                    }

                    namespace SubNamespace
                    {
                        public class Organization
                        {
                            public string Name { get; set; }
                        }
                    }
                }

                namespace SecondNamespace
                {
                    public enum PersonStatus
                    {
                        Active,
                        Inactive
                    }
                }

                namespace ThirdNamespace
                {
                    using FirstNamespace;
                    using FirstNamespace.SubNamespace;
                    using SecondNamespace;
                    using ThirdNamespace.SubThirdNamespace;

                    namespace SubThirdNamespace
                    {
                        public enum PersonType
                        {
                            Employee,
                            Customer
                        }
                    }

                    public class Person
                    {
                        public string FirstName { get; set; }
                        public PersonType PersonType { get; set; }
                        public Organization Organization { get; set; }
                        public Address Address { get; set; }
                        public PersonStatus PersonStatus { get; set; }
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 11);

            string personTypeModel = """
                namespace TargetNamespace
                {
                    public enum PersonTypeModel
                    {
                        Employee = 0,
                        Customer = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ThirdNamespace.SubThirdNamespace.PersonTypeModel.g.cs", personTypeModel, outputCompilation);

            string personTypeMapper = """
                using System;
                using ThirdNamespace.SubThirdNamespace;

                namespace TargetNamespace
                {
                    public static class PersonTypeMapper
                    {
                        public static PersonTypeModel ToModel(this PersonType value)
                        {
                            return value switch
                            {
                                PersonType.Employee => PersonTypeModel.Employee,
                                PersonType.Customer => PersonTypeModel.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static PersonType ToDomain(this PersonTypeModel value)
                        {
                            return value switch
                            {
                                PersonTypeModel.Employee => PersonType.Employee,
                                PersonTypeModel.Customer => PersonType.Customer,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ThirdNamespace.SubThirdNamespace.PersonTypeMapper.g.cs", personTypeMapper, outputCompilation);

            string addressModel = """
                namespace TargetNamespace
                {
                    public class AddressModel
                    {
                        public string City { get; set; }
                        public int Number { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.FirstNamespace.AddressModel.g.cs", addressModel, outputCompilation);

            string addressMapper = """
                using FirstNamespace;

                namespace TargetNamespace
                {
                    public static class AddressMapper
                    {
                        public static AddressModel ToModel(this Address value)
                        {
                            AddressModel model = new AddressModel()
                            {
                                City = value.City,
                                Number = value.Number,

                            };

                            return model;
                        }

                        public static Address ToDomain(this AddressModel value)
                        {
                            Address domain = new Address()
                            {
                                City = value.City,
                                Number = value.Number,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.FirstNamespace.AddressMapper.g.cs", addressMapper, outputCompilation);

            string personModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public TargetNamespace.PersonTypeModel PersonType { get; set; }
                        public TargetNamespace.OrganizationModel Organization { get; set; }
                        public TargetNamespace.AddressModel Address { get; set; }
                        public TargetNamespace.PersonStatusModel PersonStatus { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ThirdNamespace.PersonModel.g.cs", personModel, outputCompilation);

            string personMapper = """
                using ThirdNamespace;

                namespace TargetNamespace
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                FirstName = value.FirstName,
                                PersonType = value.PersonType.ToModel(),
                                Organization = value.Organization?.ToModel(),
                                Address = value.Address?.ToModel(),
                                PersonStatus = value.PersonStatus.ToModel(),

                            };

                            return model;
                        }

                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                FirstName = value.FirstName,
                                PersonType = value.PersonType.ToDomain(),
                                Organization = value.Organization?.ToDomain(),
                                Address = value.Address?.ToDomain(),
                                PersonStatus = value.PersonStatus.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.ThirdNamespace.PersonMapper.g.cs", personMapper, outputCompilation);

            string organizationModel = """
                namespace TargetNamespace
                {
                    public class OrganizationModel
                    {
                        public string Name { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.FirstNamespace.SubNamespace.OrganizationModel.g.cs", organizationModel, outputCompilation);

            string organizationMapper = """
                using FirstNamespace.SubNamespace;

                namespace TargetNamespace
                {
                    public static class OrganizationMapper
                    {
                        public static OrganizationModel ToModel(this Organization value)
                        {
                            OrganizationModel model = new OrganizationModel()
                            {
                                Name = value.Name,

                            };

                            return model;
                        }

                        public static Organization ToDomain(this OrganizationModel value)
                        {
                            Organization domain = new Organization()
                            {
                                Name = value.Name,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.FirstNamespace.SubNamespace.OrganizationMapper.g.cs", organizationMapper, outputCompilation);

            string personStatusModel = """
                namespace TargetNamespace
                {
                    public enum PersonStatusModel
                    {
                        Active = 0,
                        Inactive = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SecondNamespace.PersonStatusModel.g.cs", personStatusModel, outputCompilation);

            string personStatusMapper = """
                using System;
                using SecondNamespace;

                namespace TargetNamespace
                {
                    public static class PersonStatusMapper
                    {
                        public static PersonStatusModel ToModel(this PersonStatus value)
                        {
                            return value switch
                            {
                                PersonStatus.Active => PersonStatusModel.Active,
                                PersonStatus.Inactive => PersonStatusModel.Inactive,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static PersonStatus ToDomain(this PersonStatusModel value)
                        {
                            return value switch
                            {
                                PersonStatusModel.Active => PersonStatus.Active,
                                PersonStatusModel.Inactive => PersonStatus.Inactive,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SecondNamespace.PersonStatusMapper.g.cs", personStatusMapper, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_WhenSourceClassIsAGenericType()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] { typeof(Classes.Person<int, string, Guid, object>) }) ]
                    public class TestClass {}
                }

                namespace Classes 
                {
                    public class Person<T, U, V, W>
                    {
                        public string FirstName { get; set; }
                        public T Element { get; set; }
                        public U Keyboard { get; set; }
                        public V Screen { get; set; }
                        public W Phone { get; set; }
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
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public int Element { get; set; }
                        public string Keyboard { get; set; }
                        public System.Guid Screen { get; set; }
                        public object Phone { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonModel.g.cs", expectedModelString, outputCompilation);

            string expectedMapperString = """
                using Classes;

                namespace Test
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person<int, string, System.Guid, object> value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                FirstName = value.FirstName,
                                Element = value.Element,
                                Keyboard = value.Keyboard,
                                Screen = value.Screen,
                                Phone = value.Phone,

                            };

                            return model;
                        }

                        public static Person<int, string, System.Guid, object> ToDomain(this PersonModel value)
                        {
                            Person<int, string, System.Guid, object> domain = new Person<int, string, System.Guid, object>()
                            {
                                FirstName = value.FirstName,
                                Element = value.Element,
                                Keyboard = value.Keyboard,
                                Screen = value.Screen,
                                Phone = value.Phone,
                
                            };
                
                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonMapper.g.cs", expectedMapperString, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_WhenSourceClassIsAGenericType_AndUsesOtherGeneratedTypes()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;
                using Enums;
                using Classes;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] { 
                        typeof(Classes.Person<Address, Size>),
                        typeof(Size),
                        typeof(Address)
                    }) ]
                    public class TestClass {}
                }

                namespace Classes 
                {
                    public class Person<T, U>
                    {
                        public string FirstName { get; set; }
                        public T FirstElement { get; set; }
                        public U SecondElement { get; set; }
                    }

                    public class Address 
                    {
                        public string Street { get; set; }
                        public int Number { get; set; }
                    }
                }

                namespace Enums
                {
                    public enum Size
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
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 7);
            string expectedPersonModelString = """
                namespace Test
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public Test.AddressModel FirstElement { get; set; }
                        public Test.SizeModel SecondElement { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonModel.g.cs", expectedPersonModelString, outputCompilation);

            string expectedPersonMapperString = """
                using Classes;

                namespace Test
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person<Classes.Address, Enums.Size> value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement?.ToModel(),
                                SecondElement = value.SecondElement.ToModel(),

                            };

                            return model;
                        }

                        public static Person<Classes.Address, Enums.Size> ToDomain(this PersonModel value)
                        {
                            Person<Classes.Address, Enums.Size> domain = new Person<Classes.Address, Enums.Size>()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement?.ToDomain(),
                                SecondElement = value.SecondElement.ToDomain(),
                
                            };
                
                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonMapper.g.cs", expectedPersonMapperString, outputCompilation);

            string expectedAddressModelString = """
                namespace Test
                {
                    public class AddressModel
                    {
                        public string Street { get; set; }
                        public int Number { get; set; }
                
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.AddressModel.g.cs", expectedAddressModelString, outputCompilation);

            string expectedAddressMapperString = """
                using Classes;

                namespace Test
                {
                    public static class AddressMapper
                    {
                        public static AddressModel ToModel(this Address value)
                        {
                            AddressModel model = new AddressModel()
                            {
                                Street = value.Street,
                                Number = value.Number,

                            };

                            return model;
                        }

                        public static Address ToDomain(this AddressModel value)
                        {
                            Address domain = new Address()
                            {
                                Street = value.Street,
                                Number = value.Number,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.AddressMapper.g.cs", expectedAddressMapperString, outputCompilation);

            string expectedSizeModelString = """
                namespace Test
                {
                    public enum SizeModel
                    {
                        Big = 0,
                        Small = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.SizeModel.g.cs", expectedSizeModelString, outputCompilation);

            string expectedSizeMapperString = """
                using System;
                using Enums;
                
                namespace Test
                {
                    public static class SizeMapper
                    {
                        public static SizeModel ToModel(this Size value)
                        {
                            return value switch
                            {
                                Size.Big => SizeModel.Big,
                                Size.Small => SizeModel.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                
                        public static Size ToDomain(this SizeModel value)
                        {
                            return value switch
                            {
                                SizeModel.Big => Size.Big,
                                SizeModel.Small => Size.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.SizeMapper.g.cs", expectedSizeMapperString, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_WhenSourceClassIsGeneric_AndUsesNestedGenerics()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;
                using Enums;
                using Classes;
                using System.Collections.Generic;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] { 
                        typeof(Classes.Person<List<Address<Guid>>, Size>),
                        typeof(Size),
                        typeof(Address<Size>)
                    }) ]
                    public class TestClass {}
                }

                namespace Classes 
                {
                    public class Person<T, U>
                    {
                        public string FirstName { get; set; }
                        public T FirstElement { get; set; }
                        public U SecondElement { get; set; }
                    }

                    public class Address<T>
                    {
                        public string Street { get; set; }
                        public int Number { get; set; }
                        public T FirstElement { get; set; }
                    }
                }

                namespace Enums
                {
                    public enum Size
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
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 7);

            string expectedPersonModelString = """
                namespace Test
                {
                    public class PersonModel
                    {
                        public string FirstName { get; set; }
                        public System.Collections.Generic.List<Classes.Address<System.Guid>> FirstElement { get; set; }
                        public Test.SizeModel SecondElement { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonModel.g.cs", expectedPersonModelString, outputCompilation);

            string expectedPersonMapperString = """
                using Classes;

                namespace Test
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size> value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement,
                                SecondElement = value.SecondElement.ToModel(),

                            };

                            return model;
                        }

                        public static Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size> ToDomain(this PersonModel value)
                        {
                            Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size> domain = new Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size>()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement,
                                SecondElement = value.SecondElement.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonMapper.g.cs", expectedPersonMapperString, outputCompilation);

            string expectedSizeModelString = """
                namespace Test
                {
                    public enum SizeModel
                    {
                        Big = 0,
                        Small = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.SizeModel.g.cs", expectedSizeModelString, outputCompilation);

            string expectedSizeMapeprString = """
                using System;
                using Enums;

                namespace Test
                {
                    public static class SizeMapper
                    {
                        public static SizeModel ToModel(this Size value)
                        {
                            return value switch
                            {
                                Size.Big => SizeModel.Big,
                                Size.Small => SizeModel.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }

                        public static Size ToDomain(this SizeModel value)
                        {
                            return value switch
                            {
                                SizeModel.Big => Size.Big,
                                SizeModel.Small => Size.Small,
                                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
                            };
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Enums.SizeMapper.g.cs", expectedSizeMapeprString, outputCompilation);

            string expectedAddressModelString = """
                namespace Test
                {
                    public class AddressModel
                    {
                        public string Street { get; set; }
                        public int Number { get; set; }
                        public Test.SizeModel FirstElement { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.AddressModel.g.cs", expectedAddressModelString, outputCompilation);

            string expectedAddressMapperString = """
                using Classes;

                namespace Test
                {
                    public static class AddressMapper
                    {
                        public static AddressModel ToModel(this Address<Enums.Size> value)
                        {
                            AddressModel model = new AddressModel()
                            {
                                Street = value.Street,
                                Number = value.Number,
                                FirstElement = value.FirstElement.ToModel(),

                            };

                            return model;
                        }

                        public static Address<Enums.Size> ToDomain(this AddressModel value)
                        {
                            Address<Enums.Size> domain = new Address<Enums.Size>()
                            {
                                Street = value.Street,
                                Number = value.Number,
                                FirstElement = value.FirstElement.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.AddressMapper.g.cs", expectedAddressMapperString, outputCompilation);
        }

        [Fact]
        public async Task GeneratesModelAndMapper_WhenClassContainsTargetEnum_AsNullableProperty()
        {
            string source = """
                using System;
                using ModelMapperGenerator.Attributes;

                namespace SourceNamespace
                {
                    public enum BoxType
                    {
                        Big,
                        Small
                    }

                    public class Person
                    {
                        public string Name { get; set; }
                        public BoxType? BoxType { get; set; }
                    }
                }

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[]
                    {
                        typeof(SourceNamespace.Person),
                        typeof(SourceNamespace.BoxType),
                    })]
                    public class Hook { }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(source);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 5);

            string expectedPersonModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string Name { get; set; }
                        public TargetNamespace.BoxTypeModel? BoxType { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonModel.g.cs", expectedPersonModel, outputCompilation);

            string expectedPersonMapper = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class PersonMapper
                    {
                        public static PersonModel ToModel(this Person value)
                        {
                            PersonModel model = new PersonModel()
                            {
                                Name = value.Name,
                                BoxType = value.BoxType?.ToModel(),

                            };

                            return model;
                        }

                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                Name = value.Name,
                                BoxType = value.BoxType?.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonMapper.g.cs", expectedPersonMapper, outputCompilation);

            string expectedBoxTypeModelString = """
                namespace TargetNamespace
                {
                    public enum BoxTypeModel
                    {
                        Big = 0,
                        Small = 1,

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxTypeModel.g.cs", expectedBoxTypeModelString, outputCompilation);

            string expectedBoxTypeMapperString = """
                using System;
                using SourceNamespace;

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
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.BoxTypeMapper.g.cs", expectedBoxTypeMapperString, outputCompilation);
        }
    }
}
