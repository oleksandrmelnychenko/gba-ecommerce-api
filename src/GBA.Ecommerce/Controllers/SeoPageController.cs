using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Common.WebApi.RoutingConfiguration.Maps.Ecommerce;
using GBA.Services.Services.Ecommerce.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.SeoInfo)]
public sealed class SeoPageController(
    IResponseFactory responseFactory,
    ISeoPageService seoPageService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(SeoSegments.GET_ALL)]
    public async Task<IActionResult> GetAll() {
        return Ok(SuccessResponseBody(await seoPageService.GetAll()));
    }

    [HttpGet]
    [AssignActionRoute(SeoSegments.GET_ALL_LOCALE)]
    public async Task<IActionResult> GetAll([FromQuery] string locale) {
        return Ok(SuccessResponseBody(await seoPageService.GetAll(locale)));
    }
}