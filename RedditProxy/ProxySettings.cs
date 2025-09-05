namespace RedditProxy;

public record ProxySettings
{
    public const string SectionName = "ProxySettings";
    public string TargetHost { get; init; } = "www.reddit.com";
    public string Protocol { get; init; } = "https";
    public int TimeoutSeconds { get; init; } = 30;
    public string[] ExcludedRequestHeaders { get; init; } = 
    [
        "Host",
        "Connection", "Keep-Alive", "Proxy-Connection", 
        "Transfer-Encoding", "Upgrade", "Accept-Encoding"
    ];
    public string[] ExcludedResponseHeaders { get; init; } = 
    [
        "Transfer-Encoding", "Connection", "Keep-Alive", "Server"
    ];
}