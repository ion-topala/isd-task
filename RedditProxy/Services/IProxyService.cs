namespace RedditProxy.Services;

public interface IProxyService
{
    Task HandleProxyRequestAsync();
}