using Microsoft.CodeAnalysis;

namespace ModelMapperGenerator.Analyzers.UnitTests.Infrastructure
{
    internal sealed class DiagnosticsAssertionContext
    {
        public required string[] ExpectedMessages { get; init; }
        public required Compilation Compilation { get; init; }
        public required int ExpectedDiagnosticsCount { get; init; }
        public required int WarningLevel { get; init; }
        public required string Id { get; init; }
        public required DiagnosticSeverity DefaultSeverity { get; init; }
        public required DiagnosticSeverity Severity { get; init; }
        public required string Category { get; init; }
        public required string MessageFormat { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
    }
}
