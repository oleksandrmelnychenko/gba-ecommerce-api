using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Services;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Products)]
public sealed class ProductsController(
    IProductService productService,
    IProductSearchService searchService,
    IElasticsearchProductSearchService esSearchService,
    IPriceCacheService priceCacheService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH)]
    [OutputCache(PolicyName = "Products", VaryByQueryKeys = ["value", "limit", "offset", "withVat"])]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAllFromSearchAsync([FromQuery] string value, [FromQuery] long limit, [FromQuery] long offset, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();
        List<FromSearchProduct> products = await productService.GetAllFromSearch(value, userNetId, limit, offset, withVat.Equals(1));

        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts = products.Select(p =>
            ProtectedSearchProduct.FromSearchProduct(p, PriceObfuscator.EncodeMultiple, timestamp)
        ).ToList();

        return Ok(SuccessResponseBody(protectedProducts));
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH_V2)]
    [OutputCache(PolicyName = "Products", VaryByQueryKeys = ["value", "limit", "offset", "withVat"])]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAllFromSearchV2Async([FromQuery] string value, [FromQuery] long limit, [FromQuery] long offset, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();
        List<FromSearchProduct> products = await productService.GetAllFromSearchV2(value, userNetId, limit, offset, withVat.Equals(1));

        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts = products.Select(p =>
            ProtectedSearchProduct.FromSearchProduct(p, PriceObfuscator.EncodeMultiple, timestamp)
        ).ToList();

        return Ok(SuccessResponseBody(protectedProducts));
    }

    /// <summary>
    /// Search V3 - Elasticsearch-backed search with SQL prices only.
    /// Product data from Elasticsearch, only prices from SQL.
    /// </summary>
    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH_V3)]
    [OutputCache(PolicyName = "Products", VaryByQueryKeys = ["value", "limit", "offset", "withVat"])]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAllFromSearchV3Async(
        [FromQuery] string value,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        [FromQuery] int withVat = 0,
        [FromQuery] bool benchmark = false,
        CancellationToken cancellationToken = default) {
        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var timings = new Dictionary<string, double>();

        if (string.IsNullOrWhiteSpace(value))
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Guid userNetId = GetUserNetId();
        timings["1_GetUserNetId"] = sw.Elapsed.TotalMilliseconds;

        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";

        // Search via Elasticsearch - returns full product documents
        sw.Restart();
        ProductSearchResultWithDocs searchResult = await searchService.SearchWithDocsAsync(value, locale, limit, offset, cancellationToken);
        timings["2_ElasticsearchSearch"] = sw.Elapsed.TotalMilliseconds;

        if (searchResult.Documents.Count == 0)
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        // For anonymous users, use pre-calculated prices from ES (uses Retail Client pricing)
        // For logged-in users, fetch client-specific prices from SQL
        sw.Restart();
        Dictionary<long, ProductPriceInfo>? prices = null;
        bool useEsPrices = userNetId == Guid.Empty;

        if (!useEsPrices) {
            List<long> productIds = searchResult.Documents.Select(d => long.Parse(d.Id)).ToList();
            prices = priceCacheService.GetPrices(
                productIds,
                userNetId,
                withVat.Equals(1),
                locale,
                ids => productService.GetPricesOnly(ids, userNetId, withVat.Equals(1), locale));
        }
        timings["3_SqlFetchPrices"] = sw.Elapsed.TotalMilliseconds;
        timings["3_PriceSource"] = useEsPrices ? 0 : 1; // 0 = ES, 1 = SQL

        // Merge ES docs with prices and apply obfuscation
        sw.Restart();
        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts = searchResult.Documents.Select(doc => {
            long id = long.Parse(doc.Id);
            if (useEsPrices) {
                // Use retail price from ES (pre-calculated with Retail Client pricing)
                decimal esPrice = withVat == 1 ? doc.RetailPriceVat : doc.RetailPrice;
                var esInfo = new ProductPriceInfo { Price = esPrice, CurrencyCode = doc.RetailCurrencyCode };
                return DocToProtectedProduct(doc, esInfo, locale, timestamp);
            } else {
                prices!.TryGetValue(id, out var priceInfo);
                return DocToProtectedProduct(doc, priceInfo, locale, timestamp);
            }
        }).ToList();
        timings["4_MergeAndObfuscate"] = sw.Elapsed.TotalMilliseconds;

        totalSw.Stop();
        timings["5_Total"] = totalSw.Elapsed.TotalMilliseconds;

        if (benchmark) {
            return Ok(new {
                Body = protectedProducts,
                Benchmark = timings,
                ProductCount = protectedProducts.Count,
                SearchSource = searchResult.IsFallback ? "SQL" : "Elasticsearch",
                SearchTimeMs = searchResult.SearchTimeMs,
                TotalMatchingDocs = searchResult.TotalCount,
                StatusCode = 200
            });
        }

        return Ok(SuccessResponseBody(protectedProducts));
    }

    /// <summary>
    /// Search V3 debug - returns internal details (exact vs stem pass).
    /// </summary>
    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH_V3_DEBUG)]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAllFromSearchV3DebugAsync(
        [FromQuery] string value,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default) {
        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        var debug = await esSearchService.SearchDebugAsync(value, locale, limit, offset, cancellationToken);
        return Ok(new { Body = debug, StatusCode = 200 });
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_ANALOGUES_BY_PRODUCT_NET_ID)]
    public async Task<IActionResult> GetAllAnaloguesByProductNetIdAsync([FromQuery] Guid netId, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();

        if (userNetId == Guid.Empty)
            return Ok(
                SuccessResponseBody(
                    await productService.GetAllAnaloguesByProductNetIdForRetail(netId)
                )
            );

        return Ok(
            SuccessResponseBody(
                await productService.GetAllAnaloguesByProductNetId(netId, userNetId, withVat.Equals(1))
            )
        );
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_COMPONENTS_BY_PRODUCT_NET_ID)]
    public async Task<IActionResult> GetAllComponentsByProductNetIdAsync([FromQuery] Guid netId, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();
        return Ok(
            SuccessResponseBody(
                await productService.GetAllComponentsByProductNetId(netId, userNetId, withVat.Equals(1))
            )
        );
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_ALL_BY_VENDOR_CODES)]
    public async Task<IActionResult> GetAllByVendorCodes([FromQuery] List<string> vendorCodes, [FromQuery] long limit = 20, [FromQuery] long offset = 0,
        [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await productService.GetAllByVendorCodes(vendorCodes, userNetId, limit, offset, withVat.Equals(1))));
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_BY_NET_ID)]
    public async Task<IActionResult> GetProductByNetId([FromQuery] Guid netId, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();

        if (userNetId == Guid.Empty) return Ok(SuccessResponseBody(await productService.GetByNetIdForRetail(netId)));

        return Ok(
            SuccessResponseBody(
                await productService.GetByNetId(
                    netId,
                    userNetId,
                    withVat.Equals(1)
                )
            )
        );
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_BY_SLUG)]
    public async Task<IActionResult> GetProductBySlugAsync([FromQuery] string slug, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();
        return Ok(
            SuccessResponseBody(
                await productService.GetProductBySlug(
                    slug,
                    userNetId,
                    withVat.Equals(1)
                )
            )
        );
    }

    [HttpGet]
    [Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
    [AssignActionRoute(ProductsSegments.GET_ALL_ORDERED_PRODUCTS)]
    public async Task<IActionResult> GetAllOrderedProducts(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] long limit,
        [FromQuery] long offset
    ) {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(
            await productService.GetAllOrderedProductsFiltered(
                from,
                to,
                limit,
                offset,
                userNetId
            )
        ));
    }

    [HttpGet]
    [Authorize]
    [AssignActionRoute(ProductsSegments.GET_ORDERED_PRODUCTS_HISTORY)]
    public async Task<IActionResult> GetOrderedProductsHistory([FromQuery] Guid netId) {
        if (netId.Equals(Guid.Empty)) return BadRequest(ErrorResponseBody("empty guid", HttpStatusCode.BadRequest));
        return Ok(SuccessResponseBody(await productService.GetAllOrderedProductsHistoryByClientNetId(netId)));
    }

    private static ProtectedSearchProduct DocToProtectedProduct(
        ProductSearchDocument doc,
        ProductPriceInfo? priceInfo,
        string locale,
        long timestamp) {
        decimal price = priceInfo?.Price ?? 0;
        string currencyCode = priceInfo?.CurrencyCode ?? "UAH";
        bool isUk = locale == "uk";

        return new ProtectedSearchProduct {
            Id = long.Parse(doc.Id),
            NetUid = Guid.TryParse(doc.NetUid, out var netUid) ? netUid : Guid.Empty,
            VendorCode = doc.VendorCode,
            Name = isUk ? (doc.NameUA.Length > 0 ? doc.NameUA : doc.Name) : (doc.Name.Length > 0 ? doc.Name : doc.NameUA),
            Description = isUk ? (doc.DescriptionUA.Length > 0 ? doc.DescriptionUA : doc.Description) : (doc.Description.Length > 0 ? doc.Description : doc.DescriptionUA),
            Size = doc.Size,
            PackingStandard = doc.PackingStandard,
            OrderStandard = doc.OrderStandard,
            UCGFEA = doc.Ucgfea,
            Volume = doc.Volume,
            Top = doc.Top,
            AvailableQtyUk = doc.AvailableQtyUk,
            AvailableQtyRoad = 0,
            AvailableQtyUkVAT = doc.AvailableQtyUkVat,
            AvailableQtyPl = doc.AvailableQtyPl,
            AvailableQtyPlVAT = doc.AvailableQtyPlVat,
            Weight = doc.Weight,
            HasAnalogue = doc.HasAnalogue,
            HasComponent = doc.HasComponent,
            HasImage = doc.HasImage,
            IsForWeb = doc.IsForWeb,
            IsForSale = doc.IsForSale,
            IsForZeroSale = doc.IsForZeroSale,
            MainOriginalNumber = doc.MainOriginalNumber,
            OriginalNumbers = doc.OriginalNumbers,
            Image = doc.Image,
            MeasureUnitId = doc.MeasureUnitId,
            // DEBUG: raw prices (encryption disabled for testing)
            P = string.Join(",", new[] { price, price, 0m, 0m }.Select(p => p.ToString("F2"))),
            CurrencyCode = currencyCode,
            T = timestamp,
            ProductSlug = doc.SlugId > 0 ? new ProductSlug {
                Id = doc.SlugId,
                NetUid = Guid.TryParse(doc.SlugNetUid, out var slugNetUid) ? slugNetUid : Guid.Empty,
                Url = doc.SlugUrl,
                Locale = doc.SlugLocale,
                ProductId = long.Parse(doc.Id)
            } : null
        };
    }

}
