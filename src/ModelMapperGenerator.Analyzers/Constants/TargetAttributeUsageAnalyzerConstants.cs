namespace ModelMapperGenerator.Analyzer.Constants
{
    internal static class TargetAttributeUsageAnalyzerConstants
    {
        public const string DiagnosticId = "MMG1";
        public const string Title = "Invalid generation targets";
        public const string MessageFormat = "There should only be a single ModelGenerationTarget attribute usage in namespace {0}";
        public const string Description = "Applying ModelGenerationTarget attribute to more than one class in a namespace can cause incorrect behavior and will prevent the generator from working.";
        public const string Category = "ModelMapperGenerator.ModelGenerationTarget";
    }
}
