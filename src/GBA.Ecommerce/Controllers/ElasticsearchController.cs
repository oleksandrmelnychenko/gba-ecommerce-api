using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Sync;
using GBA.Common.IdentityConfiguration.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, "elasticsearch")]
[Authorize(Roles = IdentityRoles.Administrator)]
public sealed class ElasticsearchController(
    IElasticsearchIndexService indexService,
    IElasticsearchSyncService syncService,
    IElasticsearchProductSearchService searchService,
    IOutputCacheStore outputCacheStore,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    private const int _defaultSearchLimit = 20;
    private const int _maxSearchLimit = 100;
    private const int _maxSearchOffset = 5000;

    [HttpGet]
    [Route("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthAsync(CancellationToken ct) {
        bool healthy = await indexService.IsHealthyAsync(ct);
        return Ok(SuccessResponseBody(new { healthy }));
    }

    [HttpPost]
    [Route("index/create")]
    public async Task<IActionResult> CreateIndexAsync(CancellationToken ct) {
        bool created = await indexService.CreateIndexAsync(ct);
        await outputCacheStore.EvictByTagAsync("products", ct);
        return Ok(SuccessResponseBody(new { created }));
    }

    [HttpDelete]
    [Route("index/delete")]
    public async Task<IActionResult> DeleteIndexAsync(CancellationToken ct) {
        bool deleted = await indexService.DeleteIndexAsync(ct);
        await outputCacheStore.EvictByTagAsync("products", ct);
        return Ok(SuccessResponseBody(new { deleted }));
    }

    [HttpPost]
    [Route("sync/full")]
    public async Task<IActionResult> FullSyncAsync(CancellationToken ct) {
        SyncResult result = await syncService.FullRebuildAsync(ct);
        await outputCacheStore.EvictByTagAsync("products", ct);
        return Ok(SuccessResponseBody(result));
    }

    [HttpPost]
    [Route("sync/incremental")]
    public async Task<IActionResult> IncrementalSyncAsync(CancellationToken ct) {
        SyncResult result = await syncService.IncrementalSyncAsync(ct);
        await outputCacheStore.EvictByTagAsync("products", ct);
        return Ok(SuccessResponseBody(result));
    }

    [HttpGet]
    [Route("search")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "AnonymousProductSearch")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default) {

        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        int esLimit = limit <= 0 ? _defaultSearchLimit : Math.Min(limit, _maxSearchLimit);
        int esOffset = offset < 0 ? 0 : Math.Min(offset, _maxSearchOffset);
        ProductSearchResult result = await searchService.SearchAsync(query, locale, esLimit, esOffset, ct);
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
        ElasticsearchDebugResult result = await searchService.SearchDebugAsync(query, locale, limit, offset, ct);
        return Ok(SuccessResponseBody(result));
    }
}
