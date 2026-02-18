using System.Threading;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Search.Elasticsearch;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, "elasticsearch")]
public sealed class ElasticsearchController(
    IElasticsearchIndexService indexService,
    IElasticsearchSyncService syncService,
    IElasticsearchProductSearchService searchService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {

    [HttpGet]
    [Route("health")]
    public async Task<IActionResult> HealthAsync(CancellationToken ct) {
        bool healthy = await indexService.IsHealthyAsync(ct);
        return Ok(SuccessResponseBody(new { healthy }));
    }

    [HttpPost]
    [Route("index/create")]
    public async Task<IActionResult> CreateIndexAsync(CancellationToken ct) {
        bool created = await indexService.CreateIndexAsync(ct);
        return Ok(SuccessResponseBody(new { created }));
    }

    [HttpDelete]
    [Route("index/delete")]
    public async Task<IActionResult> DeleteIndexAsync(CancellationToken ct) {
        bool deleted = await indexService.DeleteIndexAsync(ct);
        return Ok(SuccessResponseBody(new { deleted }));
    }

    [HttpPost]
    [Route("sync/full")]
    public async Task<IActionResult> FullSyncAsync(CancellationToken ct) {
        var result = await syncService.FullRebuildAsync(ct);
        return Ok(SuccessResponseBody(result));
    }

    [HttpPost]
    [Route("sync/incremental")]
    public async Task<IActionResult> IncrementalSyncAsync(CancellationToken ct) {
        var result = await syncService.IncrementalSyncAsync(ct);
        return Ok(SuccessResponseBody(result));
    }

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default) {

        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        var result = await searchService.SearchAsync(query, locale, limit, offset, ct);
        return Ok(SuccessResponseBody(result));
    }

    [HttpGet]
    [Route("search/debug")]
    public async Task<IActionResult> SearchDebugAsync(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default) {

        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        var result = await searchService.SearchDebugAsync(query, locale, limit, offset, ct);
        return Ok(SuccessResponseBody(result));
    }
}
