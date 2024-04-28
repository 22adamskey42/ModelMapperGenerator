namespace ModelMapperGenerator.Analyzers.Constants
{
    internal static class OpenGenericsNotSupportedAnalyzerConstants
    {
        public const string DiagnosticId = "MMG4";
        public const string Title = "Open generics are not supported";
        public const string MessageFormat = "Typeof expression used in ModelGenerationTargetAttribute is an open generic, which is not supported";
        public const string Description = "Open generics are not supported by the source generator.";
        public const string Category = "ModelMapperGenerator.ModelGenerationTarget";
    }
}
