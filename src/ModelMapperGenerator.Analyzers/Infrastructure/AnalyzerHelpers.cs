using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Analyzer.Constants;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModelMapperGenerator.Analyzers.Infrastructure
{
    internal static class AnalyzerHelpers
    {
        public static bool IsTargetAttribute(ref SyntaxNodeAnalysisContext context)
        {
            ISymbol containingSymbol = context.ContainingSymbol;
            ImmutableArray<AttributeData> attribs = containingSymbol.GetAttributes();
            foreach (AttributeData attribute in attribs)
            {
                if (attribute.AttributeClass.Name == AttributeConstants.TargetAttributeName &&
                    attribute.AttributeClass.ToDisplayString() == AttributeConstants.TargetAttributeFullyQualifiedName)
                {
                    return true;
                }
            }

            return false;
        }

        public static INamedTypeSymbol? GetSymbolFromSyntaxNode(ref SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            if (node is not TypeOfExpressionSyntax)
            {
                return null;
            }

            TypeOfExpressionSyntax typeOfSyntax = (TypeOfExpressionSyntax)node;
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(typeOfSyntax.Type);
            return symbolInfo.Symbol as INamedTypeSymbol;
        }
    }
}
