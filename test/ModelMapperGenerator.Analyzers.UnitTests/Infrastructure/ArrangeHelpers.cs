using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.TestInfrastructure;
using System.Collections.Immutable;

namespace ModelMapperGenerator.Analyzer.UnitTests.Infrastructure
{
    internal static class ArrangeHelpers
    {
        public static (CompilationWithAnalyzers, Compilation) ArrangeTest(string generationSource, DiagnosticAnalyzer analyzer)
        {
            Compilation compilation = CompilationBuilder.CreateCompilation(generationSource);
            ImmutableArray<DiagnosticAnalyzer> analyzers = [analyzer];
            CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
            return (compilationWithAnalyzers, compilation);
        }
    }
}
