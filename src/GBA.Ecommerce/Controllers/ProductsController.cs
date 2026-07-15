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
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Products)]
public sealed class ProductsController(
    IProductService productService,
    IElasticsearchProductSearchService esSearchService,
    ISearchServingGenerationResolver servingGenerationResolver,
    IPriceCacheService priceCacheService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    private const int _defaultSearchLimit = 20;
    private const int _maxSearchLimit = 100;
    private const int _maxSearchOffset = 5000;
    private const int _authenticatedCandidateBatchSize = 1000;
    private const int _maxAuthenticatedCandidates = 10_000;

    [HttpGet]
    [AssignActionRoute(ProductsSegments.SEARCH)]
    [EnableRateLimiting("search")]
    public Task<IActionResult> GetAllFromSearchAsync([FromQuery] string value, [FromQuery] long limit, [FromQuery] long offset, [FromQuery] int withVat = 0, CancellationToken cancellationToken = default) =>
        SearchServingRequestGuard.ExecuteAsync(
            servingGenerationResolver,
            generation => SearchWithElasticsearchAsync(
                value,
                limit,
                offset,
                withVat,
                generation,
                cancellationToken),
            cancellationToken);

    private async Task<IActionResult> SearchWithElasticsearchAsync(
        string value,
        long limit,
        long offset,
        int withVat,
        SearchActiveGeneration servingGeneration,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(value))
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        Guid userNetId = GetUserNetId();
        string locale = RouteData.Values["culture"]?.ToString() ?? "uk";
        bool requestedWithVat = withVat.Equals(1);
        bool isAnonymous = userNetId == Guid.Empty;

        ProductPricingContext pricingContext = productService.GetPricingContext(userNetId, requestedWithVat);
        if (pricingContext == null || pricingContext.WithVat != requestedWithVat)
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        PricingDependencyRevisions pricingRevisions = pricingContext.DependencyRevisions;
        bool useIndexedRetailPrices = false;
        if (isAnonymous && pricingRevisions.IsValid) {
            useIndexedRetailPrices =
                servingGeneration.HasExactIndexedPricingRevisions(pricingRevisions);
        }

        ProductSearchCatalogContext catalogContext = new(
            pricingContext.OrganizationId,
            pricingContext.Source,
            pricingContext.WithVat,
            pricingContext.ClientAgreementNetId,
            pricingContext.PricingId.GetValueOrDefault(),
            pricingContext.CurrencyId.GetValueOrDefault(),
            useIndexedRetailPrices,
            pricingRevisions);
        if (!catalogContext.IsValid)
            return Ok(SuccessResponseBody(new List<ProtectedSearchProduct>()));

        int esLimit = limit <= 0 ? _defaultSearchLimit : (int)Math.Min(limit, _maxSearchLimit);
        int esOffset = offset < 0 ? 0 : (int)Math.Min(offset, _maxSearchOffset);

        long timestamp = PriceObfuscator.GetTimestamp();
        List<ProtectedSearchProduct> protectedProducts;

        if (useIndexedRetailPrices) {
            ProductSearchResultWithDocs searchResult = await esSearchService.SearchWithDocsAsync(
                value,
                catalogContext,
                locale,
                esLimit,
                esOffset,
                cancellationToken);

            protectedProducts = searchResult.Documents
                .Where(document => (requestedWithVat
                    ? document.RetailPriceVat
                    : document.RetailPrice) > 0)
                .Select(document => {
                    decimal retailPrice = requestedWithVat
                        ? document.RetailPriceVat
                        : document.RetailPrice;
                    ProductPriceInfo retailPriceInfo = new() {
                        Price = retailPrice,
                        CurrencyCode = requestedWithVat
                            ? document.RetailCurrencyCodeVat
                            : document.RetailCurrencyCode
                    };
                    return DocToProtectedProduct(document, retailPriceInfo, locale, timestamp);
                })
                .ToList();
        } else {
            int requiredEligibleCount = checked(esOffset + esLimit);
            int candidateOffset = 0;
            int candidateTotal = int.MaxValue;
            HashSet<long> seenProductIds = [];
            List<(ProductSearchDocument Document, ProductPriceInfo Price)> eligible = [];

            while (candidateOffset < candidateTotal
                   && candidateOffset < _maxAuthenticatedCandidates
                   && eligible.Count < requiredEligibleCount) {
                int candidateLimit = Math.Min(
                    _authenticatedCandidateBatchSize,
                    _maxAuthenticatedCandidates - candidateOffset);
                ProductSearchResultWithDocs candidatePage = await esSearchService.SearchWithDocsAsync(
                    value,
                    catalogContext,
                    locale,
                    candidateLimit,
                    candidateOffset,
                    cancellationToken);
                candidateTotal = candidatePage.TotalCount;
                if (candidatePage.Documents.Count == 0) break;

                List<ProductSearchDocument> uniqueDocuments = candidatePage.Documents
                    .Where(document => document.Id > 0 && seenProductIds.Add(document.Id))
                    .ToList();
                List<long> productIds = uniqueDocuments.Select(document => document.Id).ToList();
                Dictionary<long, ProductPriceInfo> prices = priceCacheService.GetPrices(
                    productIds,
                    userNetId,
                    pricingContext,
                    locale,
                    ids => productService.GetPricesOnly(ids, pricingContext, locale));

                foreach (ProductSearchDocument document in uniqueDocuments) {
                    if (prices.TryGetValue(document.Id, out ProductPriceInfo? price)
                        && price != null
                        && price.Price > 0) {
                        eligible.Add((document, price));
                    }
                }

                candidateOffset += candidatePage.Documents.Count;
            }

            bool exactRequestedPageIsKnown = eligible.Count >= requiredEligibleCount
                                             || candidateOffset >= candidateTotal;
            protectedProducts = exactRequestedPageIsKnown
                ? eligible
                    .Skip(esOffset)
                    .Take(esLimit)
                    .Select(item => DocToProtectedProduct(item.Document, item.Price, locale, timestamp))
                    .ToList()
                : [];
        }

        return Ok(SuccessResponseBody(protectedProducts));
    }

    [HttpGet]
    [AssignActionRoute(ProductsSegments.GET_ANALOGUES_BY_PRODUCT_NET_ID)]
    public async Task<IActionResult> GetAllAnaloguesByProductNetIdAsync([FromQuery] Guid netId, [FromQuery] int withVat = 0) {
        Guid userNetId = GetUserNetId();

        if (userNetId == Guid.Empty)
            return Ok(
                SuccessResponseBody(
                    await productService.GetAllAnaloguesByProductNetIdForRetail(netId, withVat.Equals(1))
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

        if (userNetId == Guid.Empty)
            return Ok(SuccessResponseBody(await productService.GetByNetIdForRetail(netId, withVat.Equals(1))));

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
        ProductPriceInfo priceInfo,
        string locale,
        long timestamp) {
        decimal price = priceInfo.Price;
        string currencyCode = priceInfo.CurrencyCode;
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
