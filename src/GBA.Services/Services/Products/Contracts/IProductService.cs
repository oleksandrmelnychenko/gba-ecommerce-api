using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.Repositories.Products;

namespace GBA.Services.Services.Products.Contracts;

public interface IProductService {
    Task<Product> GetProductBySlug(string slug, Guid clientNetId, bool withVat);

    Task<Product> GetByNetId(Guid productNetId, Guid clientAgreementNetId, bool withVat);

    Task<Product> GetByNetIdForRetail(Guid productNetId);

    Task<List<FromSearchProduct>> GetAllFromSearch(string value, Guid currentClientNetId, long limit, long offset, bool withVat);

    Task<List<FromSearchProduct>> GetAllFromSearchV2(string value, Guid currentClientNetId, long limit, long offset, bool withVat);

    Task<List<FromSearchProduct>> GetAllAnaloguesByProductNetId(Guid productNetId, Guid currentClientNetId, bool withVat);

    Task<List<FromSearchProduct>> GetAllAnaloguesByProductNetIdForRetail(Guid productNetId);

    Task<List<FromSearchProduct>> GetAllComponentsByProductNetId(Guid productNetId, Guid currentClientNetId, bool withVat);

    Task<List<Product>> GetAllByVendorCodes(List<string> vendorCodes, Guid currentClientNetId, long limit, long offset, bool withVat);

    Task<List<OrderItem>> GetAllOrderedProductsFiltered(DateTime from, DateTime to, long limit, long offset, Guid clientNetId);

    Task<List<ProductHistoryModel>> GetAllOrderedProductsHistoryByClientNetId(Guid netId);

    /// <summary>
    /// Gets products by IDs with calculated prices for the specified client.
    /// Used by Typesense search to fetch full product data after getting IDs from search index.
    /// </summary>
    Task<List<FromSearchProduct>> GetAllByIds(List<long> productIds, Guid currentClientNetId, bool withVat);

    /// <summary>
    /// Gets only calculated prices for products (lightweight query for V3 search).
    /// Product data comes from Typesense, this only calculates client-specific prices.
    /// </summary>
    Dictionary<long, ProductPriceInfo> GetPricesOnly(List<long> productIds, Guid currentClientNetId, bool withVat, string culture = "uk");
}