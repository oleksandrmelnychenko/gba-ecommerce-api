using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.CarBrands)]
public sealed class CarBrandsController(
    ICarBrandService carBrandService,
    IStringLocalizer<CarBrandsController> localizer,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(CarBrandsSegments.GET_ALL_PRODUCTS_FILTERED)]
    public async Task<IActionResult> GetAllProductsFilteredAsync(
        [FromQuery] Guid carBrandNetId,
        [FromQuery] string carBrandAlias,
        [FromQuery] long limit = 20,
        [FromQuery] long offset = 0) {
        try {
            Guid userNetId = GetUserNetId();

            return Ok(
                SuccessResponseBody(
                    string.IsNullOrEmpty(carBrandAlias)
                        ? await carBrandService.GetAllProductsFilteredByCarBrand(
                            carBrandNetId,
                            userNetId,
                            limit,
                            offset
                        )
                        : await carBrandService.GetAllProductsFilteredByCarBrand(
                            carBrandAlias,
                            userNetId,
                            limit,
                            offset
                        )
                )
            );
        } catch (Exception exc) {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\error_log.txt";

            string logData =
                $"\r\n Route: {$"{ControllerContext.HttpContext.Request.Path}{ControllerContext.HttpContext.Request.QueryString}"} \r\n Triggered at {DateTime.UtcNow.ToString()} UTC \r\n";

            await System.IO.File.AppendAllTextAsync(logFilePath, logData);

            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(localizer[exc.Message], HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(CarBrandsSegments.GET_ALL_CAR_BRANDS)]
    public async Task<IActionResult> GetAllCarBrandsAsync() {
        try {
            return Ok(
                SuccessResponseBody(
                    await carBrandService.GetAllCarBrands()
                )
            );
        } catch (Exception exc) {
            string logFilePath = $"{ConfigurationManager.EnvironmentRootPath}\\Data\\error_log.txt";

            string logData =
                $"\r\n Route: {$"{ControllerContext.HttpContext.Request.Path}{ControllerContext.HttpContext.Request.QueryString}"} \r\n Triggered at {DateTime.UtcNow.ToString()} UTC \r\n";

            await System.IO.File.AppendAllTextAsync(logFilePath, logData);

            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}