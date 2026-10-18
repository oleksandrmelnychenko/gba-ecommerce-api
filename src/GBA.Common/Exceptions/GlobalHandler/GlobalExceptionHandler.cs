using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Logging;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.Exceptions.GlobalHandler.Contracts;
using GBA.Common.Exceptions.UserExceptions.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using NLog;
using HttpBadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

namespace GBA.Common.Exceptions.GlobalHandler;

/// <summary>
/// Global exception handler.
/// Write the log if exception is fatal.
/// </summary>
public class GlobalExceptionHandler : IGlobalExceptionHandler {
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly Logger _logger;

    /// <summary>
    /// ctor().
    /// </summary>
    public GlobalExceptionHandler() {
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Handle all kind of exceptions ( Server, User, etc. )
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="exceptionHandlerFeature"></param>
    /// <returns></returns>
    public async Task HandleException(HttpContext httpContext, IExceptionHandlerFeature exceptionHandlerFeature) {
        //Unhandler sever exceptions.
        await HandleServerException(httpContext, exceptionHandlerFeature);
    }

    private async Task HandleServerException(HttpContext context, IExceptionHandlerFeature exceptionHandler) {
        bool isRouteConstraint = exceptionHandler.Error is IRouteContraintException;
        bool isInvalidRequest = exceptionHandler.Error is ArgumentException or JsonException;
        bool isForbidden = exceptionHandler.Error is UnauthorizedAccessException;
        bool isBadHttpRequest = TryGetBadHttpRequestStatusCode(exceptionHandler.Error, out HttpStatusCode badRequestStatusCode);
        HttpStatusCode statusCode = isRouteConstraint || isForbidden
            ? HttpStatusCode.Forbidden
            : isBadHttpRequest
                ? badRequestStatusCode
            : isInvalidRequest
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError;

        string correlationId = context.GetCorrelationId();

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        string errorMessage = exceptionHandler.Error is IRouteContraintException routeException
            ? routeException.GetUserMessageException
            : isForbidden
                ? "Access is forbidden."
            : isBadHttpRequest
                ? statusCode == HttpStatusCode.RequestEntityTooLarge
                    ? "The request is too large."
                    : "The request is invalid."
            : isInvalidRequest
                ? "The request is invalid."
                : "An unexpected error occurred.";

        ErrorResponse response = new() {
            Body = null,
            Message = errorMessage,
            StatusCode = statusCode,
            CorrelationId = correlationId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = null
        })).ConfigureAwait(false);

        LogEventInfo logEvent = new(GetLogLevel(statusCode), _logger.Name, exceptionHandler.Error.Message) {
            Exception = exceptionHandler.Error
        };
        logEvent.Properties[LoggingDefaults.CorrelationIdProperty] = correlationId;
        logEvent.Properties["RequestMethod"] = context.Request.Method;
        logEvent.Properties["RequestPath"] = context.Request.Path.Value;
        logEvent.Properties["UserNetId"] = context.GetUserNetId();
        logEvent.Properties["StatusCode"] = (int)statusCode;
        _logger.Log(logEvent);
    }

    private static bool TryGetBadHttpRequestStatusCode(Exception exception, out HttpStatusCode statusCode) {
        int rawStatusCode = exception switch {
            HttpBadHttpRequestException httpException => httpException.StatusCode,
            _ => 0
        };

        if (rawStatusCode is >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError) {
            statusCode = (HttpStatusCode)rawStatusCode;
            return true;
        }

        statusCode = default;
        return false;
    }

    private static LogLevel GetLogLevel(HttpStatusCode statusCode) {
        return (int)statusCode >= (int)HttpStatusCode.InternalServerError
            ? LogLevel.Error
            : LogLevel.Warn;
    }
}
