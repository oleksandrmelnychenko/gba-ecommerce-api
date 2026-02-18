using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.ExchangeRates.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.ExchangeRates)]
public sealed class ExchangeRatesController(
    IExchageRateService exchangeRateService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(ExchangeRatesSegments.GET_BY_CURRENT_CULTURE)]
    public async Task<IActionResult> GetProductByNetId([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await exchangeRateService.GetAllByCurrentCulture()));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}