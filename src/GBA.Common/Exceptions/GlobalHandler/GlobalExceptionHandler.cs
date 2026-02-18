using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder;
using GBA.Common.Exceptions.GlobalHandler.Contracts;
using GBA.Common.Exceptions.UserExceptions.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using NLog;

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
    /// <param name="isDevelopmentMode"></param>
    /// <returns></returns>
    public async Task HandleException(HttpContext httpContext, IExceptionHandlerFeature exceptionHandlerFeature, bool isDevelopmentMode) {
        //Unhandler sever exceptions.
        await HandleServerException(httpContext, exceptionHandlerFeature, isDevelopmentMode);
    }

    private async Task HandleServerException(HttpContext context, IExceptionHandlerFeature exceptionHandler, bool isDevelopment) {
        HttpStatusCode statusCode = exceptionHandler.Error is IRouteContraintException
            ? HttpStatusCode.Forbidden
            : HttpStatusCode.BadRequest;

        string developerMessage = string.Format(CultureInfo.CurrentCulture,
            $"{exceptionHandler.Error.Message}</br>{exceptionHandler.Error.InnerException}</br>{exceptionHandler.Error.StackTrace}");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        string errorMessage = exceptionHandler.Error is IRouteContraintException routeException
            ? routeException.GetUserMessageException
            : exceptionHandler.Error.Message;

        ErrorResponse response = new() {
            Body = null,
            Message = isDevelopment ? developerMessage : errorMessage,
            StatusCode = statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = null
        })).ConfigureAwait(false);

        _logger.Error(exceptionHandler.Error, developerMessage);
    }
}