using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Sync;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    ISearchServingGenerationResolver servingGenerationResolver,
    IProductService productService,
    IOutputCacheStore outputCacheStore,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    private const int _defaultSearchLimit = 20;
    private const int _maxSearchLimit = 100;
    private const int _maxSearchOffset = 5000;

    [HttpGet]
    [Route("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthAsync(CancellationToken ct) {
        ElasticsearchHealthReport health = await indexService.GetHealthAsync(ct);
        SearchServingGenerationResolution syncReadiness =
            await servingGenerationResolver.ResolveAsync(ct);

        bool ready = health.Status == ElasticsearchHealthStatus.Healthy
                     && syncReadiness.IsAvailable;

        object body = SuccessResponseBody(new {
            healthy = ready,
            status = ready ? "healthy" : "unhealthy",
            generationStatus = health.Status.ToString().ToLowerInvariant(),
            health.ClusterAvailable,
            health.ClusterStatus,
            health.HasActiveGeneration,
            health.PointedIndexExists,
            health.ConfigurationConsistent,
            health.PricingRevisionsCurrent,
            health.AliasConsistent,
            Reasons = health.Reasons.Concat(syncReadiness.Reasons).ToArray(),
            syncStateReadable = syncReadiness.SyncStateReadable,
            schemaCurrent = syncReadiness.SchemaCurrent,
            hasWatermark = syncReadiness.HasWatermark,
            lastSyncUtc = syncReadiness.LastSyncUtc,
            lagSeconds = syncReadiness.LagSeconds,
            stale = syncReadiness.Stale,
            incrementalCatchUpRequired = syncReadiness.IncrementalCatchUpRequired,
            lastFullRebuildStartedUtc = syncReadiness.LastFullRebuildStartedUtc,
            lastIncrementalCatchUpUtc = syncReadiness.LastIncrementalCatchUpUtc
        });

        return ready
            ? Ok(body)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }

    [HttpPost]
    [Route("index/create")]
    public IActionResult CreateIndex() {
        return Conflict(ErrorResponseBody(
            "Direct index creation is disabled. Run the fenced full sync.",
            System.Net.HttpStatusCode.Conflict));
    }

    [HttpDelete]
    [Route("index/delete")]
    public IActionResult DeleteIndex() {
        return Conflict(ErrorResponseBody(
            "Direct index deletion is disabled. Generations are retired only by fenced cleanup.",
            System.Net.HttpStatusCode.Conflict));
    }

    [HttpPost]
    [Route("sync/full")]
    public async Task<IActionResult> FullSyncAsync(CancellationToken ct) {
        SyncResult result = await syncService.FullRebuildAsync(ct);
        if (!result.Success) {
            IWebResponse failure = ErrorResponseBody(
                result.Error ?? "Full Elasticsearch rebuild failed.",
                System.Net.HttpStatusCode.ServiceUnavailable);
            failure.Body = result;
            return StatusCode(StatusCodes.Status503ServiceUnavailable, failure);
        }

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
    [EnableRateLimiting("search")]
    public Task<IActionResult> SearchAsync(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        [FromQuery] int withVat = 0,
        CancellationToken ct = default) =>
        SearchServingRequestGuard.ExecuteAsync(
            servingGenerationResolver,
            generation => SearchCoreAsync(query, limit, offset, withVat, generation, ct),
            ct);

    private async Task<IActionResult> SearchCoreAsync(
        string query,
        int limit,
        int offset,
        int withVat,
        SearchActiveGeneration servingGeneration,
        CancellationToken ct) {
        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        int esLimit = limit <= 0 ? _defaultSearchLimit : Math.Min(limit, _maxSearchLimit);
        int esOffset = offset < 0 ? 0 : Math.Min(offset, _maxSearchOffset);
        bool requestedWithVat = withVat == 1;
        ProductPricingContext pricingContext = productService.GetPricingContext(Guid.Empty, requestedWithVat);
        if (pricingContext == null || pricingContext.WithVat != requestedWithVat) {
            return Ok(SuccessResponseBody(ProductSearchResult.Empty));
        }

        PricingDependencyRevisions pricingRevisions = pricingContext.DependencyRevisions;
        if (!pricingRevisions.IsValid) {
            return Ok(SuccessResponseBody(ProductSearchResult.Empty));
        }

        if (!servingGeneration.HasExactIndexedPricingRevisions(pricingRevisions)) {
            return Ok(SuccessResponseBody(ProductSearchResult.Empty));
        }

        ProductSearchCatalogContext catalogContext = new(
            pricingContext.OrganizationId,
            pricingContext.Source,
            pricingContext.WithVat,
            pricingContext.ClientAgreementNetId,
            pricingContext.PricingId.GetValueOrDefault(),
            pricingContext.CurrencyId.GetValueOrDefault(),
            UseIndexedRetailPrice: true,
            pricingRevisions);
        ProductSearchResult result = catalogContext.IsValid
            ? await searchService.SearchAsync(query, catalogContext, locale, esLimit, esOffset, ct)
            : ProductSearchResult.Empty;
        return Ok(SuccessResponseBody(result));
    }

    [HttpGet]
    [Route("search/debug")]
    public Task<IActionResult> SearchDebugAsync(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default) =>
        SearchServingRequestGuard.ExecuteAsync(
            servingGenerationResolver,
            _ => SearchDebugCoreAsync(query, limit, offset, ct),
            ct);

    private async Task<IActionResult> SearchDebugCoreAsync(
        string query,
        int limit,
        int offset,
        CancellationToken ct) {
        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        ElasticsearchDebugResult result = await searchService.SearchDebugAsync(query, locale, limit, offset, ct);
        return Ok(SuccessResponseBody(result));
    }
}
