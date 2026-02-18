using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Common.WebApi.RoutingConfiguration.Maps.Ecommerce;
using GBA.Services.Services.Ecommerce.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.SeoInfo)]
public sealed class SeoPageController(
    IResponseFactory responseFactory,
    ISeoPageService seoPageService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(SeoSegments.GET_ALL)]
    public async Task<IActionResult> GetAll() {
        try {
            return Ok(SuccessResponseBody(await seoPageService.GetAll()));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(SeoSegments.GET_ALL_LOCALE)]
    public async Task<IActionResult> GetAll([FromQuery] string locale) {
        try {
            return Ok(SuccessResponseBody(await seoPageService.GetAll(locale)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}