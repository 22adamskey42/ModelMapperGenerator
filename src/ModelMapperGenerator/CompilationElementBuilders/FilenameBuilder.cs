using ModelMapperGenerator.Descriptors;
using System;
using ModelMapperGenerator.Constants;

namespace ModelMapperGenerator.CompilationElementBuilders
{
    internal static class FilenameBuilder
    {
        public static string BuildFileName(string generatedName, NamedTypeSymbolDescriptor sourceDescriptor, TargetAttributeDescriptor targetAttributeDescriptor)
        {
            int totalLen = targetAttributeDescriptor.TargetNamespace.Length + 1 + sourceDescriptor.SymbolNamespaceName.Length + 1 + generatedName.Length + 1 + SourceElementsConstants.GCs.Length;

            Span<char> chars = stackalloc char[totalLen];
            int currentPosition = 0;
            CopyToSpan(ref chars, ref currentPosition, targetAttributeDescriptor.TargetNamespace);
            CopyToSpan(ref chars, ref currentPosition, sourceDescriptor.SymbolNamespaceName);
            CopyToSpan(ref chars, ref currentPosition, generatedName);
            CopyToSpan(ref chars, ref currentPosition, SourceElementsConstants.GCs);

            string createdString = chars.ToString();
            return createdString;
        }

        private static void CopyToSpan(ref Span<char> targetSpan, ref int position, string sourceString)
        {
            Span<char> subSpan = targetSpan.Slice(position, sourceString.Length);
            sourceString.AsSpan().CopyTo(subSpan);
            position += subSpan.Length;
            if (sourceString != SourceElementsConstants.GCs)
            {
                targetSpan[position++] = SourceElementsConstants.Dot;
            }
        }
    }
}
