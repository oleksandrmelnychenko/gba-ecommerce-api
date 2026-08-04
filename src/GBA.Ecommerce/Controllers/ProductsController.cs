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
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Services;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Products)]
public sealed class ProductsController(
    IProductService productService,
    IElasticsearchProductSearchService esSearchService,
    IPriceCacheService priceCacheService,
    IClientResourceAccessService clientResourceAccessService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    private const int _defaultSearchLimit = 20;
    private const int _maxSearchLimit = 100;
    private const int _maxSearchOffset = 5000;
    private const int _defaultSeoIndexLimit = 1000;
    private const int _maxSeoIndexLimit = 5000;

    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH)]
    [OutputCache(PolicyName = "AnonymousProductSearch")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAllFromSearchAsync([FromQuery] string value, [FromQuery] long limit, [FromQuery] long offset, [FromQuery] int withVat = 0, CancellationToken cancellationToken = default) {
        return await SearchWithElasticsearchAsync(value, limit, offset, withVat, cancellationToken);
    }

    private async Task<IActionResult> SearchWithElasticsearchAsync(string value, long limit, long offset, int withVat, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(value))
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        Guid userNetId = GetUserNetId();
        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";

        int esLimit = limit <= 0 ? _defaultSearchLimit : (int)Math.Min(limit, _maxSearchLimit);
        int esOffset = offset < 0 ? 0 : (int)Math.Min(offset, _maxSearchOffset);

        ProductSearchResultWithDocs searchResult = await esSearchService.SearchWithDocsAsync(value, locale, esLimit, esOffset, cancellationToken);

        if (searchResult.Documents.Count == 0)
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        // Elasticsearch is the catalog/search projection, not the pricing authority.
        // Resolve all prices in one SQL batch so anonymous retail prices use the active
        // ecommerce storage/agreement (including its currency and VAT mode) exactly like
        // the product details endpoint. The per-product cache keeps repeated searches fast.
        List<long> productIds = searchResult.Documents.Select(d => d.Id).ToList();
        ProductPricingContext pricingContext =
            productService.GetPricingContext(userNetId, withVat.Equals(1));
        Dictionary<long, ProductPriceInfo> prices = priceCacheService.GetPrices(
            productIds,
            userNetId,
            withVat.Equals(1),
            locale,
            pricingContext.CacheKey,
            ids => productService.GetPricesOnly(ids, userNetId, withVat.Equals(1), locale));

        // Stock is more volatile than the search document. Always overlay it from SQL using the
        // same storage scope cart reservation uses; otherwise a targeted re-index race can expose
        // stock that checkout cannot reserve.
        Dictionary<long, double> sellableQuantities =
            productService.GetSellableQuantities(productIds, userNetId, locale);

        string fallbackCurrencyCode = pricingContext.CurrencyCode;

        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts = searchResult.Documents.Select(doc => {
            prices.TryGetValue(doc.Id, out ProductPriceInfo? priceInfo);
            sellableQuantities.TryGetValue(doc.Id, out double sellableQuantity);
            ProductPriceInfo resolvedPrice = ResolveSearchPrice(
                priceInfo,
                pricingContext.CurrencyCode);
            return DocToProtectedProduct(
                doc,
                resolvedPrice,
                locale,
                timestamp,
                ResolveVisibleSearchQuantity(resolvedPrice, sellableQuantity),
                fallbackCurrencyCode);
        }).ToList();

        return Ok(SuccessResponseBody(protectedProducts));
    }

    internal static ProductPriceInfo ResolveSearchPrice(
        ProductPriceInfo? calculatedPrice,
        string configuredCurrencyCode) {
        return calculatedPrice ?? new ProductPriceInfo {
            Price = 0,
            LocalPrice = 0,
            CurrencyCode = configuredCurrencyCode ?? string.Empty
        };
    }

    internal static double ResolveVisibleSearchQuantity(
        ProductPriceInfo? authoritativePrice,
        double sellableQuantity) {
        return authoritativePrice?.Price > 0m && double.IsFinite(sellableQuantity)
            ? Math.Max(0d, sellableQuantity)
            : 0d;
    }

    /// <summary>
    /// Returns a stable, lightweight list of public product URLs for sitemap generation.
    /// The response intentionally excludes prices, availability and client-specific data.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [AssignActionRoute(ProductsSegments.SEO_INDEX)]
    [OutputCache(PolicyName = "ProductSeoIndex")]
    [EnableRateLimiting("seo-index")]
    public async Task<IActionResult> GetSeoIndex(
        [FromQuery] int limit = _defaultSeoIndexLimit,
        [FromQuery] long offset = 0) {
        int safeLimit = limit <= 0
            ? _defaultSeoIndexLimit
            : Math.Min(limit, _maxSeoIndexLimit);
        long safeOffset = Math.Max(offset, 0);

        return Ok(SuccessResponseBody(
            await productService.GetSeoIndex(safeLimit, safeOffset)));
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
        if (!clientResourceAccessService.CanAccessClient(GetUserNetId(), netId)) return Forbid();

        return Ok(SuccessResponseBody(await productService.GetAllOrderedProductsHistoryByClientNetId(netId)));
    }

    private static ProtectedSearchProduct DocToProtectedProduct(
        ProductSearchDocument doc,
        ProductPriceInfo? priceInfo,
        string locale,
        long timestamp,
        double sellableQuantity,
        string fallbackCurrencyCode) {
        decimal price = priceInfo?.Price ?? 0;
        decimal localPrice = priceInfo?.LocalPrice ?? 0;
        string currencyCode = priceInfo?.CurrencyCode ?? fallbackCurrencyCode;
        bool isUk = locale == "uk";

        return new ProtectedSearchProduct {
            Id = doc.Id,
            NetUid = Guid.TryParse(doc.NetUid, out Guid netUid) ? netUid : Guid.Empty,
            VendorCode = doc.VendorCode,
            Name = isUk ? (doc.NameUA.Length > 0 ? doc.NameUA : doc.Name) : (doc.Name.Length > 0 ? doc.Name : doc.NameUA),
            Description = isUk ? (doc.DescriptionUA.Length > 0 ? doc.DescriptionUA : doc.Description) : (doc.Description.Length > 0 ? doc.Description : doc.DescriptionUA),
            Size = doc.Size,
            PackingStandard = doc.PackingStandard,
            OrderStandard = doc.OrderStandard,
            UCGFEA = doc.Ucgfea,
            Volume = doc.Volume,
            Top = doc.Top,
            AvailableQtyUk = sellableQuantity,
            AvailableQtyRoad = 0,
            AvailableQtyUkVAT = sellableQuantity,
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
            // Prices are encrypted with the shared price key and never shipped as cleartext. The
            // slots are [current, local, currentWithVat, localWithVat]; this endpoint resolves the
            // price in exactly one VAT mode (effectiveWithVat above), so the VAT slots carry the
            // same VAT-resolved values instead of the hardcoded 0.00 the debug path emitted.
            P = PriceObfuscator.EncodeMultiple(new[] { price, localPrice, price, localPrice }, timestamp),
            CurrencyCode = currencyCode,
            T = timestamp,
            ProductSlug = doc.SlugId > 0 ? new ProductSlug {
                Id = doc.SlugId,
                NetUid = Guid.TryParse(doc.SlugNetUid, out Guid slugNetUid) ? slugNetUid : Guid.Empty,
                Url = doc.SlugUrl,
                Locale = doc.SlugLocale,
                ProductId = doc.Id
            } : null
        };
    }

}
