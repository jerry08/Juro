using HtmlAgilityPack;

namespace Juro.Utils;

internal static class Html
{
    private static readonly HtmlDocument HtmlDocument = new();

    public static HtmlDocument Parse(string source)
    {
        HtmlDocument.LoadHtml(HtmlEntity.DeEntitize(source));
        return HtmlDocument;
    }
}