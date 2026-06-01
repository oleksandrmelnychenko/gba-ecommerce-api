using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GBA.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GBA.Common.Middleware;

/// <summary>
/// Logs one structured line per request (method, path, status, elapsed, user, correlation id).
/// For failed requests (status >= 400) it also captures the (truncated) request body and query
/// string so a failure can be diagnosed from logs alone. Sensitive fields (passwords, tokens)
/// are redacted from both before they reach the log.
/// </summary>
public sealed class RequestLoggingMiddleware {
    private const string SecretKeys = "password|pass|pwd|token|refresh_?token|access_?token|secret|authorization|api_?key";

    private static readonly Regex JsonSecretRegex = new(
        "(\"(?:" + SecretKeys + ")\"\\s*:\\s*)\"[^\"]*\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FormSecretRegex = new(
        "((?:^|[?&])(?:" + SecretKeys + ")=)[^&]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<RequestLoggingMiddleware> logger) {
        bool bodyBuffered = TryEnableBodyBuffering(context);
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
                string body = bodyBuffered ? Redact(await ReadBufferedBodyAsync(context)) : string.Empty;
                string query = Redact(context.Request.QueryString.Value ?? string.Empty);

                const string template =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs:0.##} ms (user {UserNetId}) query={QueryString} body={RequestBody}";

                if (statusCode >= 500) {
                    logger.LogError(template, method, path, statusCode, elapsedMs, userNetId, query, body);
                } else {
                    logger.LogWarning(template, method, path, statusCode, elapsedMs, userNetId, query, body);
                }
            } else {
                logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs:0.##} ms (user {UserNetId})",
                    method, path, statusCode, elapsedMs, userNetId);
            }
        }
    }

    private static string Redact(string value) {
        if (string.IsNullOrEmpty(value)) return value;

        string result = JsonSecretRegex.Replace(value, "$1\"***\"");
        result = FormSecretRegex.Replace(result, "$1***");
        return result;
    }

    private static bool TryEnableBodyBuffering(HttpContext context) {
        HttpRequest request = context.Request;

        if (request.ContentLength is null or 0 or > LoggingDefaults.MaxLoggedBodyBytes) return false;

        string contentType = request.ContentType ?? string.Empty;
        bool isTextual =
            contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("text", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("urlencoded", StringComparison.OrdinalIgnoreCase);

        if (!isTextual) return false;

        request.EnableBuffering();
        return true;
    }

    private static async Task<string> ReadBufferedBodyAsync(HttpContext context) {
        Stream body = context.Request.Body;
        if (!body.CanSeek) return string.Empty;

        long originalPosition = body.Position;
        body.Position = 0;

        try {
            using StreamReader reader = new(body, leaveOpen: true);
            char[] buffer = new char[LoggingDefaults.MaxLoggedBodyBytes];
            int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            return new string(buffer, 0, read);
        } catch {
            return string.Empty;
        } finally {
            body.Position = originalPosition;
        }
    }
}
