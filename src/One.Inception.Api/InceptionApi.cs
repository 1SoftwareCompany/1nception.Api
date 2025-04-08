using System;
using System.Net;
using One.Inception.Discoveries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace One.Inception.Api;

public class ApiBuilder
{
    public Func<IServiceCollection, IConfiguration, InceptionServicesProvider> ServicesProvider { get; set; }

    public IConfigurationSource AdditionalConfigurationSource { get; set; }
}

public static class InceptionApi
{
    private static readonly ILogger logger = InceptionLogger.CreateLogger(typeof(InceptionApi));

    public static IHost GetHost(Action<ApiBuilder> builder = null) => GetHostBuilder(builder).Build();

    public static IHostBuilder GetHostBuilder(Action<ApiBuilder> builder = null)
    {
        var apiBuilder = new ApiBuilder();
        if (builder != null)
            builder(apiBuilder);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation($"Starting Inception API.{Environment.NewLine}If you are not able to access it using DNS or public IP make sure that you have properly configured your firewall rules.");

        var hostBuilder = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddInception(context.Configuration);
                if (apiBuilder.ServicesProvider is null == false)
                    services.AddInception(apiBuilder.ServicesProvider(services, context.Configuration));
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                if (apiBuilder.AdditionalConfigurationSource is null == false)
                    webBuilder.ConfigureAppConfiguration(cfg => cfg.Add(apiBuilder.AdditionalConfigurationSource));

                webBuilder.UseKestrel((context, options) =>
                {
                    IConfigurationSection kestrelSection = context.Configuration.GetSection("Inception:Api:Kestrel");
                    if (kestrelSection.Exists())
                    {
                        options.Configure(kestrelSection);
                    }
                    else
                    {
                        options.Listen(IPAddress.Any, 7477, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    }
                });

                webBuilder.UseStartup<Startup>();
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                options.ValidateOnBuild = false;
            });

        return hostBuilder;
    }
}
