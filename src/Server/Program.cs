using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Types;
using Server;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddCommandLine(args);

builder.Logging.AddConsole(o =>
{
    // Log everything
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddSingleton<SettingsLoader>()
    .AddSingleton<NL2KQLClientService>()
    .AddSingleton<KustoService>()
    .AddSingleton<ResourceService>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithSubscribeToResourcesHandler(async (ctx, ct) =>
    {
        if (ctx?.Params?.Uri is not null)
        {
            var resourceService = ctx.Server.Services!.GetRequiredService<ResourceService>();
            resourceService.AddSubscription(ctx.Params.Uri);
        }

        return new EmptyResult();
    })
    .WithUnsubscribeFromResourcesHandler(async (ctx, ct) =>
    {
        if (ctx?.Params?.Uri is not null)
        {
            var resourceService = ctx.Server.Services!.GetRequiredService<ResourceService>();
            resourceService.RemoveSubscription(ctx.Params.Uri);
        }

        return new EmptyResult();
    })
    .WithListResourcesHandler(async (ctx, ct) =>
    {
        var resourceService = ctx.Server.Services!.GetRequiredService<ResourceService>();
        return new ListResourcesResult
        {
            Resources = resourceService.List(),
        };
    })
    .WithReadResourceHandler(async (ctx, ct) =>
    {
        if (ctx?.Params?.Uri is null)
        {
            throw new ArgumentNullException(nameof(ctx.Params.Uri));
        }

        var resourceService = ctx.Server.Services!.GetRequiredService<ResourceService>();
        var resource = resourceService.GetResource(ctx.Params.Uri);
        if (resource is null)
        {
            throw new KeyNotFoundException($"Resource with uri {ctx.Params.Uri} not found.");
        }

        return new ReadResourceResult
        {
            Contents =
            [
                Utilities.IsBinary(resource.MimeType)
                    ? new BlobResourceContents
                    {
                        MimeType = resource.MimeType,
                        Uri = resource.Uri,
                    }
                    : new TextResourceContents
                    {
                        MimeType = resource.MimeType,
                        Uri = resource.Uri
                    }
                ],
        };
    });

// Build the host
var host = builder.Build();

// Force the settings to load to get an error early
try
{
    var settingsLoader = host.Services.GetRequiredService<SettingsLoader>();
    settingsLoader.Get();
}
catch (Exception ex)
{
    host.Services.GetRequiredService<ILogger<Program>>().LogError("Could not load settings: {ErrorMessage}", ex.Message);
    return;
}

// Run the application
await host.RunAsync();
