using System;
using System.Threading.Tasks;
using GBA.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NLog;

namespace GBA.Common.Middleware;

/// <summary>
/// Assigns a correlation id to every request (honouring an inbound <c>X-Correlation-ID</c>
/// header when present), echoes it back on the response, and pushes it onto the NLog
/// scope so that every log line emitted while handling the request carries it.
/// </summary>
public sealed class CorrelationIdMiddleware {
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        string correlationId = ResolveCorrelationId(context);

        context.Items[LoggingDefaults.CorrelationIdItemKey] = correlationId;
        context.Response.Headers[LoggingDefaults.CorrelationIdHeader] = correlationId;

        using (ScopeContext.PushProperty(LoggingDefaults.CorrelationIdProperty, correlationId)) {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context) {
        if (context.Request.Headers.TryGetValue(LoggingDefaults.CorrelationIdHeader, out StringValues inbound)) {
            string candidate = inbound.ToString();
            if (!string.IsNullOrWhiteSpace(candidate)) return candidate.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
