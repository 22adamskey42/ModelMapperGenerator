using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ModelMapperGenerator
{
    internal sealed class SymbolDescriptor
    {
        public INamedTypeSymbol Symbol { get; private set; }
        public string SymbolName { get; private set; }
        public ImmutableHashSet<string> SymbolMembers { get; private set; }
        public string SymbolNamespace { get; private set; }
        public string TargetNamespace { get; private set; }

        public SymbolDescriptor(INamedTypeSymbol symbol, string targetNamespace)
        {
            Symbol = symbol;
            SymbolName = symbol.Name;
            SymbolMembers = symbol.MemberNames.ToImmutableHashSet();
            SymbolNamespace = symbol.ContainingNamespace.ToDisplayString();
            TargetNamespace = targetNamespace;
        }
    }
}
