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
    IElasticsearchProductSearchService esSearchService,
    IPriceCacheService priceCacheService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    private const int _defaultSearchLimit = 20;
    private const int _maxSearchLimit = 100;
    private const int _maxSearchOffset = 5000;

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

        Dictionary<long, ProductPriceInfo>? prices = null;
        bool useEsPrices = userNetId == Guid.Empty;

        if (!useEsPrices) {
            List<long> productIds = searchResult.Documents.Select(d => d.Id).ToList();
            prices = priceCacheService.GetPrices(
                productIds,
                userNetId,
                withVat.Equals(1),
                locale,
                ids => productService.GetPricesOnly(ids, userNetId, withVat.Equals(1), locale));
        }

        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts = searchResult.Documents.Select(doc => {
            long id = doc.Id;
            if (useEsPrices) {
                decimal esPrice = withVat == 1 ? doc.RetailPriceVat : doc.RetailPrice;
                ProductPriceInfo esInfo = new ProductPriceInfo { Price = esPrice, CurrencyCode = doc.RetailCurrencyCode };
                return DocToProtectedProduct(doc, esInfo, locale, timestamp);
            } else {
                prices!.TryGetValue(id, out ProductPriceInfo? priceInfo);
                return DocToProtectedProduct(doc, priceInfo, locale, timestamp);
            }
        }).ToList();

        return Ok(SuccessResponseBody(protectedProducts));
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
                NetUid = Guid.TryParse(doc.SlugNetUid, out Guid slugNetUid) ? slugNetUid : Guid.Empty,
                Url = doc.SlugUrl,
                Locale = doc.SlugLocale,
                ProductId = doc.Id
            } : null
        };
    }

}
