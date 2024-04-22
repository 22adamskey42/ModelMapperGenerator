namespace ModelMapperGenerator.Analyzer.Constants
{
    internal static class RecordsNotSupportedAnalyzerConstants
    {
        public const string DiagnosticId = "MMG2";
        public const string Title = "Records are not supported";
        public const string MessageFormat = "Type {0} used in ModelGenerationTargetAttribute is a record, which is not supported";
        public const string Description = "Record types are not supported and will be ignored by the source generator.";
        public const string Category = "ModelMapperGenerator.ModelGenerationSource";
    }
}
