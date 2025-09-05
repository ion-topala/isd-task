namespace RedditProxy.Services;

public interface IHtmlProcessingService
{
    string ProcessHtmlContent(string html, string targetHost, string proxyHost, string requestScheme);
}