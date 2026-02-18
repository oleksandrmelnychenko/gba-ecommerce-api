using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.SalesModels;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;
using GBA.Domain.EntityHelpers.SalesModels.Models;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderItemRepository {
    long Add(OrderItem orderItem);

    long AddOneTimeDiscount(OrderItem orderItem);

    void Add(IEnumerable<OrderItem> orderItems);

    void Update(OrderItem orderItem);

    void Update(IEnumerable<OrderItem> orderItems);

    void UpdateItemAssignment(OrderItem orderItem);

    void UpdateQty(OrderItem orderItem);

    void UpdateOverLoadQty(OrderItem orderItem);

    void UpdateInvoiceDocumentQty(OrderItem orderItem);

    void UpdateReturnedQty(OrderItem orderItem);

    void UpdatePricePerItem(OrderItem orderItem);

    void UpdateOneTimeDiscount(OrderItem orderItem);

    void UpdateOneTimeDiscountComment(OrderItem orderItem);

    void Remove(OrderItem orderItem);

    void Remove(Guid netId);

    void Remove(IEnumerable<OrderItem> orderItems);

    void SetItemsZeroOffered(IEnumerable<OrderItem> items);

    void SetUnpackedQtyToAllItemsByOrderId(long orderId);

    void SetOfferProcessingStatuses(IEnumerable<OrderItem> items);

    void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids);

    void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids);

    void DecreaseUnpackedQtyById(long id, double qty);

    void AssignSpecification(OrderItem orderItem);

    OrderItem GetByNetId(Guid netId);

    OrderItem GetByNetIdWithProduct(Guid netId);

    OrderItem GetByNetIdWithoutIncludes(Guid netId);

    OrderItem GetFilteredByIds(long orderId, long productId, bool reSaleAvailability);

    OrderItem GetById(long id);

    OrderItem GetByIdWithClientInfo(long id);

    OrderItem GetByIdWithClientAgreement(long id);

    OrderItem GetByIdWithIncludes(long id, Guid clientAgreementNetId);

    OrderItem GetByIdWithIncludes(long id, Guid? clientAgreementNetId, Guid? vatAgreementNetId);

    OrderItem GetBySaleNetIdAndOrderItemId(Guid saleNetId, long orderItemId);

    OrderItem GetBySaleNetIdAndOrderItemId(Guid saleNetId, long orderItemId, long organizationId);

    OrderItem GetWithCalculatedProductPrices(Guid netId, Guid clientAgreementNetId, long organizationId, bool vatSale, bool reSale);

    OrderItem GetWithCalculatedProductPrices(long id, Guid clientAgreementNetId, long organizationId, bool vatSale, bool reSale);

    OrderItem GetByIdAndClientAgreementNetIdWithIncludes(long id, Guid clientAgreementNetId, long currencyId);

    OrderItem GetOrderItemByOrderProductAndSpecificationIfExits(
        long orderId,
        long productId,
        string specificationName,
        string specificationCode,
        string specificationLocale,
        decimal specificationDutyPercent,
        bool isFromReSale);

    IEnumerable<OrderItem> GetAllFromCurrentShoppingByClientNetId(long? workplaceId, Guid clientAgreementNetId, long? currencyId, long? organizationId, bool withVat);

    IEnumerable<OrderItem> GetAllFromCurrentShoppingByClientNetId(Guid clientNetId, Guid? clientAgreementNetId, Guid? vatAgreementNetId, bool withVat);

    List<OrderItem> GetAllBySaleNetIdWithProductLocation(Guid netId);

    List<OrderItem> GetAllWithProductMovementsBySaleId(long id);

    List<OrderItem> GetAllWithConsignmentMovementBySaleId(long saleId);

    Dictionary<DateTime, decimal?> GetChartInfoSalesByClient(
        DateTime from,
        DateTime to,
        long clientId,
        TypePeriodGrouping typePeriod);

    InfoAboutSalesModel GetInfoAboutSales(
        DateTime from,
        DateTime to,
        long? managerId,
        long? organizationId);

    AllProductsSaleManagersModel GetManagersProductSalesByTop(DateTime from, DateTime to, TypeOfProductTop typeProductTop);

    decimal GetReSalePricePerItem(Guid productNetId, Guid clientAgreementNetId, long orderItemId);

    OrderItem GetOrderItemByOrderProductAndSpecification(
        long orderId,
        long productId,
        ProductSpecification productSpecification,
        bool isFromReSale);

    IEnumerable<OrderItem> GetByIdsWithClientAgreement(IEnumerable<long> ids);
}