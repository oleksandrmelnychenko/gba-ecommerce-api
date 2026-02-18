using System.Globalization;
using System.Net;
using System.Threading.Tasks;
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
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "text/html";

        string developerMessage = string.Format(CultureInfo.CurrentCulture,
            $"{exceptionHandler.Error.Message}</br>{exceptionHandler.Error.InnerException}</br>{exceptionHandler.Error.StackTrace}");

        if (isDevelopment) {
            await context.Response.WriteAsync(developerMessage).ConfigureAwait(false);
        } else if (exceptionHandler.Error is IRouteContraintException) {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync((exceptionHandler.Error as IRouteContraintException).GetUserMessageException).ConfigureAwait(false);
        }

        _logger.Fatal(developerMessage);
    }
}