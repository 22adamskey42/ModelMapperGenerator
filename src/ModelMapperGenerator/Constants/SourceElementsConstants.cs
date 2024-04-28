namespace ModelMapperGenerator.Constants
{
    internal static class SourceElementsConstants
    {
        public const char Whitespace = ' ';
        public const char Dot = '.';
        public const char Comma = ',';
        public const char LesserThan = '<';
        public const char GreaterThan = '>';
        public const char QuestionMark = '?';
        public const char OpeningBrace = '{';
        public const char ClosingBrace = '}';
        public const char Semicolon = ';';
        public const char GenericT = 'T';
        public const string Namespace = "namespace";
        public const string PublicStaticClass = "public static class";
        public const string Using = "using";
        public const string Arrow = " => ";
        public const string Model = "Model";
        public const string Mapper = "Mapper";
        public const string GetSet = "{ get; set; }";
        public const string Assignment = " = ";
        public const string Value = "value.";
        public const string ToModel = ".ToModel()";
        public const string ToModelNullSafe = "?.ToModel()";
        public const string ToDomain = ".ToDomain()";
        public const string ToDomainNullSafe = "?.ToDomain()";
        public const string Indent = "    ";
        public const string DoubleIndent = Indent + Indent;
        public const string TripleIndent = DoubleIndent + Indent;
        public const string QuadrupleIndent = TripleIndent + Indent;
        public const string GCs = "g.cs";
        public const string Public = "public ";
        public const string UnknownEnum = QuadrupleIndent + "_ => throw new ArgumentOutOfRangeException(\"Unknown enum value\")";
    }
}
