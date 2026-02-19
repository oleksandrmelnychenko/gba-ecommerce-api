using System;
using System.Net;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Common.WebApi;

/// <summary>
/// Base controller.
/// </summary>
public abstract class WebApiControllerBase : Controller {
    private readonly IResponseFactory _responseFactory;

    /// <summary>
    /// ctor().
    /// </summary>
    protected WebApiControllerBase(IResponseFactory responseFactory) {
        Logger = LogManager.GetCurrentClassLogger();

        _responseFactory = responseFactory;
    }

    /// <summary>
    /// Nlogger
    /// </summary>
    protected Logger Logger { get; }

    protected IWebResponse SuccessResponseBody(object body, string message = "") {
        IWebResponse response = _responseFactory.GetSuccessReponse();

        response.Body = body;
        response.StatusCode = HttpStatusCode.OK;
        response.Message = message;

        return response;
    }

    protected IWebResponse ErrorResponseBody(string message, HttpStatusCode statusCode) {
        IWebResponse response = _responseFactory.GetErrorResponse();

        response.Message = message;
        response.StatusCode = statusCode;

        return response;
    }

    /// <summary>
    /// Gets the authenticated user's NetId from HttpContext.
    /// </summary>
    /// <returns>The user's NetId as Guid, or Guid.Empty if not authenticated.</returns>
    protected Guid GetUserNetId() {
        if (HttpContext.Items.TryGetValue(UserNetIdMiddleware.NetIdKey, out object value) && value is Guid netId) {
            return netId;
        }
        return Guid.Empty;
    }
}