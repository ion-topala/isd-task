using RedditProxy;
using RedditProxy.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ProxySettings>(builder.Configuration.GetSection(ProxySettings.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IHtmlProcessingService, HtmlProcessingService>();
builder.Services.AddHttpClient<IProxyService, ProxyService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) => true;
    }

    return handler;
});

var app = builder.Build();

app.MapFallback(async (IProxyService proxyService) =>
{
    await proxyService.HandleProxyRequestAsync();
}); 

app.Run();