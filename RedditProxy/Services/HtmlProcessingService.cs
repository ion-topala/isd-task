using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace RedditProxy.Services;

public partial class HtmlProcessingService : IHtmlProcessingService
{
    public string ProcessHtmlContent(string html, string targetHost, string proxyHost, string requestScheme)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        ReplaceUrlsInAttributes(doc, targetHost, proxyHost, requestScheme);
        ProcessTextNodes(doc.DocumentNode);

        return doc.DocumentNode.OuterHtml;
    }

    private static void ReplaceUrlsInAttributes(HtmlDocument doc, string targetHost, string proxyHost,
        string requestScheme)
    {
        var elementsWithUrls = doc.DocumentNode.SelectNodes("//*[@href or @src or @action or @data-url]");
        if (elementsWithUrls != null)
        {
            foreach (var element in elementsWithUrls)
            {
                foreach (var attribute in element.Attributes)
                {
                    if (IsUrlAttribute(attribute.Name))
                    {
                        var originalUrl = attribute.Value;
                        var newUrl = ReplaceRedditUrls(originalUrl, targetHost, proxyHost, requestScheme);
                        attribute.Value = newUrl;
                    }
                }
            }
        }
    }

    private static void ProcessTextNodes(HtmlNode node)
    {
        if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            node.InnerHtml = SixLetterWordRegex().Replace(
                WebUtility.HtmlEncode(HtmlEntity.DeEntitize(node.InnerText)), "$0™");
        }

        foreach (var childNode in node.ChildNodes)
        {
            ProcessTextNodes(childNode);
        }
    }

    private static bool IsUrlAttribute(string attributeName)
    {
        var urlAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "action", "data-url", "data-src", "srcset"
        };

        return urlAttributes.Contains(attributeName);
    }

    private static string ReplaceRedditUrls(string content, string targetHost, string proxyHost,
        string protocol = "https")
    {
        if (string.IsNullOrEmpty(content)) return content;

        content = content.Replace($"https://{targetHost}", $"{protocol}://{proxyHost}");
        content = content.Replace($"http://{targetHost}", $"{protocol}://{proxyHost}");
        content = content.Replace($"//{targetHost}", $"//{proxyHost}");

        return content;
    }

    [GeneratedRegex(@"\b[a-zA-Z]{6}\b"),]
    private static partial Regex SixLetterWordRegex();
}