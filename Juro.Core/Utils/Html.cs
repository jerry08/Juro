using HtmlAgilityPack;

namespace Juro.Core.Utils;

internal static class Html
{
    public static HtmlDocument Parse(string source)
    {
        var document = new HtmlDocument();
        document.LoadHtml(HtmlEntity.DeEntitize(source));
        return document;
    }
}
