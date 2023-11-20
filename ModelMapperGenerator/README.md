# ModelMapperGenerator

This source generator can generate enums mirroring a specified enum and provide mappings between them with extension methods.

Given the following code:
```C#
namespace SomeNamespace;

public enum Vehicle 
{
  Car,
  Boat,
  Plane
}
```
the generator will generate:
```C#
public enum VehicleModel
{
  Car,
  Boat,
  Plane
}

public static class VehicleMapper
{
  public VehicleModel ToModel(this Vehicle value)
  {
    return value switch {
      Vehicle.Car => VehicleModel.Car,
      Vehicle.Boat => VehicleModel.Boat,
      Vehicle.Plane => VehicleModel.Plane,
      _ => throw new ArgumentOutOfRangeException("Unknown enum value.")
    };
  }

  public Vehicle ToDomain(this VehicleModel value)
  {
    return value switch {
      VehicleModel.Car => Vehicle.Car,
      VehicleModel.Boat => Vehicle.Boat,
      VehicleModel.Plane => Vehicle.Plane,
      _ => throw new ArgumentOutOfRangeException("Unknown enum value.")
    };
  }
}
```
Usage:
```C#
namespace TargetNamespaceInWhichToGenerateCode;

// attribute comes from ModelMapperGenerator.Attributes nuget, requires fully qualified type
[ModelGenerationTarget(FullyQualifiedTypes = new Type[] { typeof(SomeNamespace.Vehicle) }) ] 
public class DummyClass { }
```

