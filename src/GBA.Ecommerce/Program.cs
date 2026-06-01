using System;
using GBA.Common.Exceptions.GlobalHandler.Contracts;
using GBA.Ecommerce;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Web;

Logger startupLogger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.WebHost.ConfigureKestrel(options => {
        options.AddServerHeader = false;

        // Connection limits
        options.Limits.MaxConcurrentConnections = 1000;
        options.Limits.MaxConcurrentUpgradedConnections = 200;
        options.Limits.MaxRequestBodySize = 52428800; // 50MB

        // Keep slow clients from occupying sockets indefinitely.
        options.Limits.MinRequestBodyDataRate = new MinDataRate(240, TimeSpan.FromSeconds(10));
        options.Limits.MinResponseDataRate = new MinDataRate(240, TimeSpan.FromSeconds(10));

        // Timeouts
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
        options.Limits.MaxRequestHeaderCount = 64;
        options.Limits.MaxRequestHeadersTotalSize = 32768;

        // HTTP/2 optimizations
        options.Limits.Http2.MaxStreamsPerConnection = 250;
        options.Limits.Http2.InitialConnectionWindowSize = 1024 * 1024; // 1MB
        options.Limits.Http2.InitialStreamWindowSize = 768 * 1024; // 768KB
        options.Limits.Http2.MaxFrameSize = 32 * 1024; // 32KB
        options.Limits.Http2.MaxRequestHeaderFieldSize = 16 * 1024; // 16KB

        options.ListenAnyIP(62506, listenOptions => {
            listenOptions.Protocols = HttpProtocols.Http1;
            if (builder.Environment.IsDevelopment()) {
                listenOptions.UseConnectionLogging();
            }
        });
    });

    Startup startup = new(builder.Environment);
    startup.ConfigureServices(builder.Services);

    WebApplication app = builder.Build();

    ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    using IServiceScope scope = app.Services.CreateScope();
    IGlobalExceptionFactory globalExceptionFactory = scope.ServiceProvider.GetRequiredService<IGlobalExceptionFactory>();

    startup.Configure(app, loggerFactory, globalExceptionFactory);

    app.Run();
} catch (Exception exception) {
    startupLogger.Fatal(exception, "Ecommerce host terminated unexpectedly during startup");
    throw;
} finally {
    LogManager.Shutdown();
}
