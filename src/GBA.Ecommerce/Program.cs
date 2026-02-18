using System;
using GBA.Common.Exceptions.GlobalHandler.Contracts;
using GBA.Ecommerce;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => {
    options.AddServerHeader = false;

    // Connection limits
    options.Limits.MaxConcurrentConnections = 2000;
    options.Limits.MaxConcurrentUpgradedConnections = 2000;
    options.Limits.MaxRequestBodySize = 52428800; // 50MB

    // Disable rate limits for high-throughput scenarios
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;

    // Timeouts
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);

    // HTTP/2 optimizations
    options.Limits.Http2.MaxStreamsPerConnection = 250;
    options.Limits.Http2.InitialConnectionWindowSize = 1024 * 1024; // 1MB
    options.Limits.Http2.InitialStreamWindowSize = 768 * 1024; // 768KB
    options.Limits.Http2.MaxFrameSize = 32 * 1024; // 32KB
    options.Limits.Http2.MaxRequestHeaderFieldSize = 16 * 1024; // 16KB

    options.ListenLocalhost(62506, listenOptions => {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
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
