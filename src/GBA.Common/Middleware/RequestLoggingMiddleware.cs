using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GBA.Common.Middleware;

/// <summary>
/// Logs one structured line per request (method, path, status, elapsed, user, correlation id).
/// Query values and request bodies are intentionally excluded because ecommerce
/// payloads contain personal, payment, and bearer-capability data.
/// </summary>
public sealed class RequestLoggingMiddleware {
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<RequestLoggingMiddleware> logger) {
        long startTimestamp = Stopwatch.GetTimestamp();

        try {
            await _next(context);
        } finally {
            double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            int statusCode = context.Response.StatusCode;
            Guid userNetId = context.GetUserNetId();
            string method = context.Request.Method;
            string path = context.Request.Path.Value;

            if (statusCode >= 400) {
                string queryKeys = string.Join(",", context.Request.Query.Keys.OrderBy(key => key));

                const string template =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs:0.##} ms (user {UserNetId}) queryKeys={QueryKeys}";

                if (statusCode >= 500) {
                    logger.LogError(template, method, path, statusCode, elapsedMs, userNetId, queryKeys);
                } else {
                    logger.LogWarning(template, method, path, statusCode, elapsedMs, userNetId, queryKeys);
                }
            } else {
                logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs:0.##} ms (user {UserNetId})",
                    method, path, statusCode, elapsedMs, userNetId);
            }
        }
    }
}
