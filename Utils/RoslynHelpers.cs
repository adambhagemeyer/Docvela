using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace Docvela.Utils;

public static class RoslynHelpers
{
    public static string GetFullTypeName(ITypeSymbol? symbol)
    {
        if (symbol == null) return "unknown";
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                     .Replace("global::", "");
    }

    public static string GetSummaryFromXmlDoc(string? xmlDoc)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return "";

        try
        {
            var xDoc = XDocument.Parse(xmlDoc);
            var summary = xDoc.Root.Element("summary")?.Value ?? "";
            return summary.Trim().Replace("\n", " ").Replace("\r", " ");
        }
        catch
        {
            return "";
        }
    }
}
