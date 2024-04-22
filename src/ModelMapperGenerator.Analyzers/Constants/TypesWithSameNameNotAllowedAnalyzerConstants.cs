namespace ModelMapperGenerator.Analyzer.Constants
{
    internal static class TypesWithSameNameNotAllowedAnalyzerConstants
    {
        public const string DiagnosticId = "MMG3";
        public const string Title = "Multiple types with the same name are not supported";
        public const string MessageFormat = "Multiple types with name {0} have been placed on the same ModelGenerationTargetAttribute, which is not supported";
        public const string Description = "Placing multiple types with the same name on the same ModelGenerationTargetAttribute is not supported, separate these types into targets placed in separate namespaces.";
        public const string Category = "ModelMapperGenerator.ModelGenerationTarget";
    }
}
