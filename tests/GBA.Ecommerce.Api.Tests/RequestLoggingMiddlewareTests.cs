using System.Reflection;
using GBA.Common.Middleware;
using GBA.Ecommerce.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RequestLoggingMiddlewareTests {
    [Fact]
    public async Task ElasticsearchHealth_ServiceUnavailable_IsWarning() {
        MethodInfo action = typeof(ElasticsearchController).GetMethod(
            nameof(ElasticsearchController.HealthAsync))!;
        DefaultHttpContext context = CreateContext(
            StatusCodes.Status503ServiceUnavailable,
            action.GetCustomAttributes(inherit: true).ToArray());
        CollectingLogger<RequestLoggingMiddleware> logger = new();
        RequestLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, logger);

        Assert.Contains(
            action.GetCustomAttributes(inherit: true),
            attribute => attribute is HealthCheckEndpointAttribute);
        Assert.Equal([LogLevel.Warning], logger.Levels);
    }

    [Theory]
    [InlineData(StatusCodes.Status503ServiceUnavailable, false)]
    [InlineData(StatusCodes.Status500InternalServerError, true)]
    public async Task UnexpectedServerFailure_RemainsError(
        int statusCode,
        bool markAsHealthCheck) {
        object[] metadata = markAsHealthCheck
            ? [new HealthCheckEndpointAttribute()]
            : [];
        DefaultHttpContext context = CreateContext(statusCode, metadata);
        CollectingLogger<RequestLoggingMiddleware> logger = new();
        RequestLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, logger);

        Assert.Equal([LogLevel.Error], logger.Levels);
    }

    private static DefaultHttpContext CreateContext(
        int statusCode,
        object[] endpointMetadata) {
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/uk/elasticsearch/health";
        context.Response.StatusCode = statusCode;
        context.SetEndpoint(new Endpoint(
            null,
            new EndpointMetadataCollection(endpointMetadata),
            "test endpoint"));
        return context;
    }

    private sealed class CollectingLogger<T> : ILogger<T> {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Levels.Add(logLevel);
        }
    }

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
