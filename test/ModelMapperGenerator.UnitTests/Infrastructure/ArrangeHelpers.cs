using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.TestInfrastructure;

namespace ModelMapperGenerator.UnitTests.Infrastructure
{
    internal static class ArrangeHelpers
    {
        public static (Compilation, GeneratorDriver) ArrangeTest(string sourceText)
        {
            Compilation inputCompilation = CompilationBuilder.CreateCompilation(sourceText);
            ModelMapperSourceGenerator testedGenerator = new();
            ISourceGenerator sourceGenerator = testedGenerator.AsSourceGenerator();
            GeneratorDriverOptions opts = new(IncrementalGeneratorOutputKind.None, true);
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: opts);
            return (inputCompilation, driver);
        }
    }
}
