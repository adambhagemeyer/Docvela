using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Docvela.Utils;

public static class RoslynHelpers
{
    public static string GetFullTypeName(ITypeSymbol? symbol)
    {
        if (symbol == null) return "unknown";
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                     .Replace("global::", "");
    }
}
