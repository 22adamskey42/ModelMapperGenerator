using Microsoft.CodeAnalysis;
using ModelMapperGenerator.UnitTests.Infrastructure;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace ModelMapperGenerator.UnitTests
{
    public class GenericClassGenerationTests
    {
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
                    public class PersonModel<T0,T1,T2,T3>
                    {
                        public string FirstName { get; set; }
                        public T0 Element { get; set; }
                        public T1 Keyboard { get; set; }
                        public T2 Screen { get; set; }
                        public T3 Phone { get; set; }

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
                        public static PersonModel<int, string, System.Guid, object> ToModel(this Person<int, string, System.Guid, object> value)
                        {
                            PersonModel<int, string, System.Guid, object> model = new PersonModel<int, string, System.Guid, object>()
                            {
                                FirstName = value.FirstName,
                                Element = value.Element,
                                Keyboard = value.Keyboard,
                                Screen = value.Screen,
                                Phone = value.Phone,

                            };

                            return model;
                        }

                        public static Person<int, string, System.Guid, object> ToDomain(this PersonModel<int, string, System.Guid, object> value)
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
        public async Task GeneratesModelAndMapper_WhenSourceIsGeneric_AndUsesTypeArgInCollectionAndProperty()
        {
            // Arrange
            string sourceText = """
                using System;
                using ModelMapperGenerator.Attributes;
                using System.Collections.Generic;

                namespace Test
                {
                    [ModelGenerationTarget(Types = new Type[] 
                    { 
                        typeof(Classes.Person<Classes.Car>),
                        typeof(Classes.Car)
                    })]
                    public class TestClass {}
                }

                namespace Classes 
                {
                    public class Car
                    {   
                        public string Make { get; set; }
                    }

                    public class Person<T>
                    {
                        public string FirstName { get; set; }
                        public T Element { get; set; }
                        public List<T> OtherElements { get; set;}
                    }
                }
                """;

            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(sourceText);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 5);
            string expectedCarModel = """
                namespace Test
                {
                    public class CarModel
                    {
                        public string Make { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.CarModel.g.cs", expectedCarModel, outputCompilation);

            string expectedCarMapper = """
                using Classes;

                namespace Test
                {
                    public static class CarMapper
                    {
                        public static CarModel ToModel(this Car value)
                        {
                            CarModel model = new CarModel()
                            {
                                Make = value.Make,

                            };

                            return model;
                        }

                        public static Car ToDomain(this CarModel value)
                        {
                            Car domain = new Car()
                            {
                                Make = value.Make,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.CarMapper.g.cs", expectedCarMapper, outputCompilation);

            string expectedPersonModel = """
                namespace Test
                {
                    public class PersonModel<T0>
                    {
                        public string FirstName { get; set; }
                        public T0 Element { get; set; }
                        public System.Collections.Generic.List<Classes.Car> OtherElements { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonModel.g.cs", expectedPersonModel, outputCompilation);

            string expectedPersonMapper = """
                using Classes;

                namespace Test
                {
                    public static class PersonMapper
                    {
                        public static PersonModel<Test.CarModel> ToModel(this Person<Classes.Car> value)
                        {
                            PersonModel<Test.CarModel> model = new PersonModel<Test.CarModel>()
                            {
                                FirstName = value.FirstName,
                                Element = value.Element?.ToModel(),
                                OtherElements = value.OtherElements,

                            };

                            return model;
                        }

                        public static Person<Classes.Car> ToDomain(this PersonModel<Test.CarModel> value)
                        {
                            Person<Classes.Car> domain = new Person<Classes.Car>()
                            {
                                FirstName = value.FirstName,
                                Element = value.Element?.ToDomain(),
                                OtherElements = value.OtherElements,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("Test.Classes.PersonMapper.g.cs", expectedPersonMapper, outputCompilation);
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
                    public class PersonModel<T0,T1>
                    {
                        public string FirstName { get; set; }
                        public T0 FirstElement { get; set; }
                        public T1 SecondElement { get; set; }

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
                        public static PersonModel<Test.AddressModel, Test.SizeModel> ToModel(this Person<Classes.Address, Enums.Size> value)
                        {
                            PersonModel<Test.AddressModel, Test.SizeModel> model = new PersonModel<Test.AddressModel, Test.SizeModel>()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement?.ToModel(),
                                SecondElement = value.SecondElement.ToModel(),

                            };

                            return model;
                        }

                        public static Person<Classes.Address, Enums.Size> ToDomain(this PersonModel<Test.AddressModel, Test.SizeModel> value)
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
                    public class PersonModel<T0,T1>
                    {
                        public string FirstName { get; set; }
                        public T0 FirstElement { get; set; }
                        public T1 SecondElement { get; set; }

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
                        public static PersonModel<System.Collections.Generic.List<Classes.Address<System.Guid>>, Test.SizeModel> ToModel(this Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size> value)
                        {
                            PersonModel<System.Collections.Generic.List<Classes.Address<System.Guid>>, Test.SizeModel> model = new PersonModel<System.Collections.Generic.List<Classes.Address<System.Guid>>, Test.SizeModel>()
                            {
                                FirstName = value.FirstName,
                                FirstElement = value.FirstElement,
                                SecondElement = value.SecondElement.ToModel(),

                            };

                            return model;
                        }

                        public static Person<System.Collections.Generic.List<Classes.Address<System.Guid>>, Enums.Size> ToDomain(this PersonModel<System.Collections.Generic.List<Classes.Address<System.Guid>>, Test.SizeModel> value)
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
                    public class AddressModel<T0>
                    {
                        public string Street { get; set; }
                        public int Number { get; set; }
                        public T0 FirstElement { get; set; }

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
                        public static AddressModel<Test.SizeModel> ToModel(this Address<Enums.Size> value)
                        {
                            AddressModel<Test.SizeModel> model = new AddressModel<Test.SizeModel>()
                            {
                                Street = value.Street,
                                Number = value.Number,
                                FirstElement = value.FirstElement.ToModel(),

                            };

                            return model;
                        }

                        public static Address<Enums.Size> ToDomain(this AddressModel<Test.SizeModel> value)
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
        public async Task GeneratesModelAndMapper_WhenGenericTypeIsUsed_WithDifferentTypeArguments()
        {
            // Arrange
            string generationSource = """
                using ModelMapperGenerator.Attributes;
                using System;

                namespace SourceNamespace
                {
                    public class Registration<T>
                    {
                        public Guid Id { get; set; }
                        public T Registrant { get; set; }
                    }

                    public class Person
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                    }

                    public class Vehicle
                    {
                        public string Make { get; set; }
                        public int Seats { get; set; }
                    }
                }

                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[]
                    {
                        typeof(SourceNamespace.Registration<SourceNamespace.Person>),
                        typeof(SourceNamespace.Registration<SourceNamespace.Vehicle>),
                        typeof(SourceNamespace.Vehicle),
                        typeof(SourceNamespace.Person)
                    })]
                    public class Hook { }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 7);
            string expectedRegistrationModel = """
                namespace TargetNamespace
                {
                    public class RegistrationModel<T0>
                    {
                        public System.Guid Id { get; set; }
                        public T0 Registrant { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.RegistrationModel.g.cs", expectedRegistrationModel, outputCompilation);

            string expectedRegistrationMapper = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class RegistrationMapper
                    {
                        public static RegistrationModel<TargetNamespace.PersonModel> ToModel(this Registration<SourceNamespace.Person> value)
                        {
                            RegistrationModel<TargetNamespace.PersonModel> model = new RegistrationModel<TargetNamespace.PersonModel>()
                            {
                                Id = value.Id,
                                Registrant = value.Registrant?.ToModel(),

                            };

                            return model;
                        }

                        public static Registration<SourceNamespace.Person> ToDomain(this RegistrationModel<TargetNamespace.PersonModel> value)
                        {
                            Registration<SourceNamespace.Person> domain = new Registration<SourceNamespace.Person>()
                            {
                                Id = value.Id,
                                Registrant = value.Registrant?.ToDomain(),

                            };

                            return domain;
                        }

                        public static RegistrationModel<TargetNamespace.VehicleModel> ToModel(this Registration<SourceNamespace.Vehicle> value)
                        {
                            RegistrationModel<TargetNamespace.VehicleModel> model = new RegistrationModel<TargetNamespace.VehicleModel>()
                            {
                                Id = value.Id,
                                Registrant = value.Registrant?.ToModel(),

                            };

                            return model;
                        }

                        public static Registration<SourceNamespace.Vehicle> ToDomain(this RegistrationModel<TargetNamespace.VehicleModel> value)
                        {
                            Registration<SourceNamespace.Vehicle> domain = new Registration<SourceNamespace.Vehicle>()
                            {
                                Id = value.Id,
                                Registrant = value.Registrant?.ToDomain(),

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.RegistrationMapper.g.cs", expectedRegistrationMapper, outputCompilation);

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
                                Age = value.Age,

                            };

                            return model;
                        }

                        public static Person ToDomain(this PersonModel value)
                        {
                            Person domain = new Person()
                            {
                                Name = value.Name,
                                Age = value.Age,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonMapper.g.cs", expectedPersonMapper, outputCompilation);

            string expectedPersonModel = """
                namespace TargetNamespace
                {
                    public class PersonModel
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.PersonModel.g.cs", expectedPersonModel, outputCompilation);

            string expectedVehicleMapper = """
                using SourceNamespace;

                namespace TargetNamespace
                {
                    public static class VehicleMapper
                    {
                        public static VehicleModel ToModel(this Vehicle value)
                        {
                            VehicleModel model = new VehicleModel()
                            {
                                Make = value.Make,
                                Seats = value.Seats,

                            };

                            return model;
                        }

                        public static Vehicle ToDomain(this VehicleModel value)
                        {
                            Vehicle domain = new Vehicle()
                            {
                                Make = value.Make,
                                Seats = value.Seats,

                            };

                            return domain;
                        }
                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.VehicleMapper.g.cs", expectedVehicleMapper, outputCompilation);

            string expectedVehicleModel = """
                namespace TargetNamespace
                {
                    public class VehicleModel
                    {
                        public string Make { get; set; }
                        public int Seats { get; set; }

                    }
                }
                """;
            await AssertHelpers.AssertGeneratedCodeAsync("TargetNamespace.SourceNamespace.VehicleModel.g.cs", expectedVehicleModel, outputCompilation);

        }

        [Fact]
        public void GeneratesNothing_WhenSourceIsUnboundGeneric()
        {
            string generationSource = """
                using ModelMapperGenerator.Attributes;
                using System;
                
                namespace SourceNamespace
                {
                    public class Registration<T>
                    {
                        public Guid Id { get; set; }
                        public T Registrant { get; set; }
                    }
                }
                
                namespace TargetNamespace
                {
                    [ModelGenerationTarget(Types = new Type[]
                    {
                        typeof(SourceNamespace.Registration<>),
                    })]
                    public class Hook { }
                }
                """;
            (Compilation inputCompilation, GeneratorDriver driver) = ArrangeHelpers.ArrangeTest(generationSource);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

            // Assert
            AssertHelpers.AssertOutputCompilation(ref diagnostics, outputCompilation, 1);
        }
    }
}
