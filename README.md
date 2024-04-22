# ModelMapperGenerator

[![Build Status](https://dev.azure.com/22adamskey42/ModelMapperGenerator/_apis/build/status%2F22adamskey42.ModelMapperGenerator?branchName=master&stageName=Run%20tests&jobName=test)](https://dev.azure.com/22adamskey42/ModelMapperGenerator/_build/latest?definitionId=2&branchName=master)

This source generator generates enums and classes based on existing enums and classes.  
It contains built in analyzers which ensure the correct usage of the generator.

## Usage example

### Types from which models and mappers should be generated
```C#
namespace SomeNamespace
{
    public enum Vehicle
    {
        Car,
        Boat,
        Plane
    }

    public class Person
    {
        public string FirstName { get; set; }
        public int Age { get; set; }
    }
}
```

### Hook class
Uses ModelGenerationTargetAttribute, coming from ModelMapperGeneration.Attributes nuget package. Generated mappers and models will be placed in this namespace
```C#
using ModelMapperGenerator.Attributes;

namespace TargetNamespace
{
    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Vehicle),
        typeof(SomeNamespace.Person)
    })]
    public class Hook { }
}
```

### The following code will be generated

#### For Person class
```C#
using SomeNamespace;

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
```
```C#
namespace TargetNamespace
{
    public class PersonModel
    {
        public string FirstName { get; set; }
        public int Age { get; set; }

    }
}
```

#### For Vehicle enum
```C#
using System;
using SomeNamespace;

namespace TargetNamespace
{
    public static class VehicleMapper
    {
        public static VehicleModel ToModel(this Vehicle value)
        {
            return value switch
            {
                Vehicle.Car => VehicleModel.Car,
                Vehicle.Boat => VehicleModel.Boat,
                Vehicle.Plane => VehicleModel.Plane,
                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
            };
        }

        public static Vehicle ToDomain(this VehicleModel value)
        {
            return value switch
            {
                VehicleModel.Car => Vehicle.Car,
                VehicleModel.Boat => Vehicle.Boat,
                VehicleModel.Plane => Vehicle.Plane,
                _ => throw new ArgumentOutOfRangeException("Unknown enum value")
            };
        }
    }
}
```

```C#
namespace TargetNamespace
{
    public enum VehicleModel
    {
        Car = 0,
        Boat = 1,
        Plane = 2,

    }
}
```

## Complex classes

If a class which is a model generation source, has publicly accessible properties of types which themselves are model generation sources, then the generated model will also include models of those classes:

### Classes and hook
```C#
namespace SomeNamespace
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }
        public Grade Grade { get; set; }
    }

    // Included as a type to generate model from
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    // NOT included as type to generate model from
    public class Grade
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}

using ModelMapperGenerator.Attributes;

namespace TargetNamespace
{
    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Person),
        typeof(SomeNamespace.Address),
    })]
    public class Hook { }
}
```

### Generated Code
Person class only, Address class omitted for brevity
```C#
namespace TargetNamespace
{
    public class PersonModel
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public TargetNamespace.AddressModel Address { get; set; } // AddressModel is used instead of an Address
        public SomeNamespace.Grade Grade { get; set; } // Grade is used - Grade type is not a source of generation

    }
}

using SomeNamespace;

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
                Address = value.Address.ToModel(), // Address to AddressModel
                Grade = value.Grade, // Grade

            };

            return model;
        }

        public static Person ToDomain(this PersonModel value)
        {
            Person domain = new Person()
            {
                Name = value.Name,
                Age = value.Age,
                Address = value.Address.ToDomain(), // AddressModel to Address
                Grade = value.Grade, // Grade

            };

            return domain;
        }
    }
}
```

## Analyzers

The package comes with built in analyzers which guard against the following usage errors:  
- Placing more than one model generation hook in a single namespace
```C#
// Error - more than one hook in a single namespace
using ModelMapperGenerator.Attributes;

namespace TargetNamespace
{
    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Vehicle),
        typeof(SomeNamespace.Person)
    })]
    public class FirstHook { }

    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Vehicle),
        typeof(SomeNamespace.Person)
    })]
    public class SecondHook { }
}
```
- Using a record as a type to generate code from
```C#
namespace SomeNamespace
{
    public record Todo(string Name, bool Completed);
}

// Warning - records are ignored by source generator
using ModelMapperGenerator.Attributes;

namespace TargetNamespace
{
    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Todo)
    })]
    public class Hook { }
}

```

- Placing types with the same name in the same generation hook  
```C#
namespace SomeNamespace
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}

namespace AnotherNamespace
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}

// Error - multiple types with the same name placed in single hook
using ModelMapperGenerator.Attributes;

namespace TargetNamespace
{
    [ModelGenerationTarget(Types = new Type[]
    {
        typeof(SomeNamespace.Person),
        typeof(AnotherNamespace.Person),
    })]
    public class Hook { }
}
```
