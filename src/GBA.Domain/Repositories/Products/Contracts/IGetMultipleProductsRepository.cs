using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IGetMultipleProductsRepository {
    IEnumerable<ProductAvailability> GetAvailabilitiesByProductNetId(Guid productNetId);

    List<dynamic> GetTopTotalPurchasedByOnlineShop();

    List<Product> GetAllByIds(IEnumerable<long> ids, long organizationId);

    List<Product> GetAll(long limit, long offset);

    List<Product> GetAll(string orderBy, long limit, long offset);

    List<Product> GetAllWithDynamicPrices(string sql, string orderBy, GetQuery query, string value, Guid clientAgreementNetId);

    List<Product> GetAllByGroupNetId(Guid netId, long limit, long offset);

    List<Product> GetAllByActiveProductSpecificationCode(string code);

    IEnumerable<Product> GetAllByVendorCodeWithActiveProductSpecification(string vendorCode);

    List<Product> GetAllProductsByCarBrandNetId(Guid carBrandNetId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId, long limit, long offset);

    List<Product> GetAllProductsByCarBrandNetId(string carBrandAlias, Guid nonVatAgreementNetId, Guid? vatAgreementNetId, long limit, long offset);

    List<Product> GetAllAnaloguesByProductNetId(Guid productNetId, Guid clientAgreementNetId, long? organizationId);

    List<Product> GetAllComponentsByProductNetId(Guid productNetId, Guid clientAgreementNetId);

    List<Product> GetAllFromSearch(string value, long limit, long offset, Guid clientAgreementNetId, bool withVat = false);

    List<Product> SearchForProductsByVendorCode(string value, long limit, long offset);

    List<ProductHistoryModel> GetAllOrderedProductsHistory(Guid clientNetId);

    IEnumerable<Product> GetByOldECommerceIdsFromSearch(IEnumerable<long> oldECommerceIds, long limit, long offset);

    IEnumerable<Product> GetAllWithoutActiveSpecificationByLocale(string locale);

    IEnumerable<Product> GetAllFilteredByActiveSpecificationNameByLocale(string name, string locale);

    IEnumerable<Product> GetAllFilteredByActiveSpecificationCodeByLocale(string code, string locale);

    List<Product> GetAllFromAdvancedSearch(
        string value,
        long limit,
        long offset,
        Guid clientAgreementNetId,
        ProductAdvancedSearchMode mode,
        ProductAdvancedSortMode sortMode,
        bool withCalculatedPrices,
        long? organizationId,
        bool withVat = false);

    //eCommerce queries

    List<SearchResult> GetAllProductIdsFromSql(string sql, dynamic props);

    List<FromSearchProduct> GetAllFromIdsInTempTable(
        string preDefinedQuery,
        Guid clientAgreementNetId,
        long? currencyId,
        long? organizationId,
        bool withVat = false,
        bool isDefault = false);

    List<FromSearchProduct> GetAllFromIdsInTempTableForRetail(
        string preDefinedQuery,
        ClientAgreement clientAgreement);

    List<FromSearchProduct> GetAllFromIdsInTempTable(
        string preDefinedQuery,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId,
        long? organizationId);

    List<FromSearchProduct> GetAllAnaloguesByProductIdWithCalculatedPrices(long productId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(
        long productId,
        Guid clientAgreementNetId,
        long? organizationId,
        long? currencyId,
        bool withVat);

    List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
        long productId,
        Guid clientAgreementNetId,
        long? organizationId,
        long? currencyId,
        bool withVat);

    List<FromSearchProduct> GetAllComponentsByProductIdWithCalculatedPrices(long productId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    List<FromSearchProduct> GetAllComponentsByProductIdWithCalculatedPrices(
        long productId,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId,
        long? organizationId);

    List<FromSearchProduct> GetProductsByOldECommerceIds(IEnumerable<long> oldECommerceIds, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    List<Product> GetAllFromIdsInPreDefinedQuery(string preDefinedQuery, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    List<Product> GetAllByOldECommerceIds(IEnumerable<long> oldECommerceIds, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    List<OrderItem> GetAllOrderedProductsFiltered(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        Guid clientNetId,
        Guid activeClientAgreementNetId,
        long? currencyId,
        long? organizationId,
        bool withVat);

    List<Product> GetAllByUpdatedDates(DateTime fromDate, DateTime toDate, int limit, int offset);

    List<Product> GetAllLimited(int limit, int offset);

    long GetTotalQty();
}