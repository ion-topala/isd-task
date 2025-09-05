using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace RedditProxy.Services;

public class ProxyService(
    HttpClient httpClient,
    IHttpContextAccessor contextAccessor,
    IHtmlProcessingService htmlProcessingService,
    IOptions<ProxySettings> settings,
    ILogger<ProxyService> logger)
    : IProxyService
{
    private readonly ProxySettings _settings = settings.Value;

    public async Task HandleProxyRequestAsync()
    {
        var context = contextAccessor.HttpContext!;
        var targetUrl =
            $"{_settings.Protocol}://{_settings.TargetHost}{context.Request.Path}{context.Request.QueryString}";

        try
        {
            var request = await CreateProxyRequest(context, targetUrl);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine(
                    $"Request failed with status code {response.StatusCode} and reason {responseMessage}");
            }

            await HandleProxyResponse(context, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Proxy error for {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);
            await HandleProxyError(context, ex);
        }
    }

    private async Task<HttpRequestMessage> CreateProxyRequest(HttpContext context, string targetUrl)
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        logger.LogDebug("Creating proxy request: {Method} {Url}", context.Request.Method, targetUrl);

        request.Headers.Host = _settings.TargetHost;

        var excludedHeaders = new HashSet<string>(_settings.ExcludedRequestHeaders, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in context.Request.Headers.Where(h => !excludedHeaders.Contains(h.Key)))
        {
            try
            {
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(key, value.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to copy header {HeaderName}: {Error}", key, ex.Message);
            }
        }

        if (context.Request.ContentLength > 0 || !string.IsNullOrEmpty(context.Request.ContentType))
        {
            logger.LogDebug("Processing request body, ContentLength: {Length}, ContentType: {Type}",
                context.Request.ContentLength, context.Request.ContentType);

            var bodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(bodyStream);
            bodyStream.Position = 0;

            request.Content = new StreamContent(bodyStream);

            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                try
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Failed to parse Content-Type '{ContentType}': {Error}",
                        context.Request.ContentType, ex.Message);
                }
            }
        }

        return request;
    }

    private async Task HandleProxyResponse(HttpContext context, HttpResponseMessage response)
    {
        logger.LogDebug("Handling proxy response: {StatusCode} {ContentType}",
            response.StatusCode, response.Content.Headers.ContentType?.MediaType);

        context.Response.StatusCode = (int)response.StatusCode;

        var excludedHeaders = new HashSet<string>(_settings.ExcludedResponseHeaders, StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers.Concat(response.Content.Headers)
                     .Where(h => !excludedHeaders.Contains(h.Key)))
        {
            try
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to copy response header {HeaderName}: {Error}", header.Key, ex.Message);
            }
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (IsHtmlContent(contentType))
        {
            var content = await response.Content.ReadAsStringAsync();
            content = htmlProcessingService.ProcessHtmlContent(content,
                _settings.TargetHost,
                context.Request.Host.ToString(),
                context.Request.Scheme);

            context.Response.ContentType = contentType + "; charset=utf-8";
            context.Response.ContentLength = null;

            await context.Response.WriteAsync(content);
        }
        else
        {
            await response.Content.CopyToAsync(context.Response.Body);
        }
    }

    private static async Task HandleProxyError(HttpContext context, Exception ex)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync($"Proxy Error: {ex.Message}");
        }
    }

    private static bool IsHtmlContent(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;

        return contentType.Contains("text/html") ||
               contentType.Contains("+html");
    }
}