using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleRepository {
    long Add(Sale sale);

    void Update(Sale sale);
    void UpdateUser(Sale sale);

    void Update(List<Sale> sales);

    void UpdateSaleExpiredDays(Sale sale);

    void UpdateSadReference(Sale sale);

    void UpdateShipmentInfo(Sale sale);

    void UpdateDiscountComment(Sale sale);

    void UpdateSaleInvoiceNumber(Sale sale);

    void UpdateTaxFreePackListReference(Sale sale);

    void SetChangedToInvoiceDateByNetId(Guid netId, long? updatedById);

    void UpdateClientAgreementByIds(long saleId, long clientAgreementId);

    void UpdateSaleCommentByNetId(Guid netId, string comment);

    void UpdateWarehousesShipmentCommentByNetId(Guid netId, string comment);

    void UpdateLockInfo(Sale sale);

    void UnlockSaleById(long id);

    double GetCalculatedTotalWeightFromConsignmentsBySaleIds(IEnumerable<long> ids);

    Sale GetById(long id, bool withDeleted = false);

    Sale GetByIdWithCalculatedDynamicPrices(long id);

    Sale GetByIdWithSaleMerged(long id);

    Sale GetByIdWithOrderItemMerged(long id);

    Sale GetByIdWithAdditionalIncludes(long id);

    Sale GetByIdWithAgreement(long id);

    Sale GetByIdWithoutIncludes(long id);

    Sale GetByIdForConsignment(long id);

    Sale GetByIdForConsignment(long id, long orderItemId);

    void SetIsPrintedFalse(long id);
    void SetIsAcceptedToPackingFalse(long id);

    void SetIsPrintedActProtocolEditFalse(long id);

    Sale GetByNetId(Guid netId);

    Sale GetByNetIdWithProductLocations(Guid netId);

    Sale GetByNetIdWithSaleMergedAndOrderItemsMerged(Guid netId);

    Sale GetByNetIdWithDeletedOrderItems(Guid netId);

    Sale GetByNetIdWithShiftedItems(Guid netId);

    Sale GetByNetIdWithAgreement(Guid netId);

    Sale GetByNetIdWithShiftedItemsWithoutAdditionalIncludes(Guid netId);

    Sale GetByNetIdWithSaleMerged(Guid netId);

    Sale GetByOrderItemNetId(Guid orderItemNetId);

    Sale GetChildSaleIdIfExist(Guid parentNetId);

    Sale GetLastNewSaleByClientAgreementNetId(Guid clientAgreementNetId);

    Sale GetLastNotMergedNewSaleByClientAgreementNetId(Guid clientAgreementNetId);

    Sale GetByOrderId(long orderId);

    Sale GetByNetIdWithAgreementOnly(Guid netId);

    Sale GetSaleBySaleNumber(string value);

    List<Sale> GetAllByIds(List<long> ids, bool withCalculatedPrices = false);

    List<long> GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(Guid clientNetId, string culture, bool withVat = false);

    List<Sale> GetAllSubClientsSalesByClientNetId(Guid clientNetId);

    List<Sale> GetAllSalesFromECommerceFromPlUkClients();

    Sale GetGroupedOrderItemByProduct(Guid orderItemNetId);

    List<Sale> GetAllSalesForReturnsFromSearch(
        DateTime from,
        DateTime to,
        string value,
        Guid netId,
        Guid? organizationNetId);

    List<Sale> GetAllRangedByLifeCycleType(
        int limit,
        int offset,
        long? clientId,
        long[] organizationIds,
        SaleLifeCycleType? saleLifeCycleType,
        DateTime from,
        DateTime to,
        Guid? userNetId = null,
        string value = "",
        bool fromShipments = false,
        Guid? retailClientNetId = null,
        bool forEcommerce = false,
        bool fastEcommerce = false
    );

    List<Sale> GetAllRanged(
        DateTime from,
        DateTime to,
        SaleLifeCycleType status = SaleLifeCycleType.Packaging
    );

    List<Sale> GetAllUkPlClientsSalesFiltered(DateTime from, DateTime to, string value);

    List<Sale> GetAllByClientNetIdFiltered(GetAllSalesByClientNetIdQuery message);

    List<SalesRegisterModel> GetAllSalesWithReturnsByClientNetIdFiltered(GetSalesRegisterByClientNetIdQuery message);

    List<Sale> GetAllByLifeCycleType(SaleLifeCycleType saleLifeCycleType);
    List<Sale> GetAllRegisterIvoiceType(DateTime from, DateTime to, string value, long offset, long limit);
    List<Sale> GetAll(string orderBy, long offset, long limit);

    List<Sale> GetAll(string sql, string orderBy, GetQuery query);

    List<Sale> GetAllByUserIds(IEnumerable<long> ids);

    List<Sale> FindClientSalesBySaleNumber(Guid clientNetId, string saleNumber);


    List<Sale> GetAllFilteredByTransporterAndType(DateTime from, DateTime to, Guid netId, bool onlyPrinted = false);

    List<Sale> GetAllPlSalesFiltered(DateTime from, DateTime to);

    IEnumerable<Sale> GetAllExpiredOrLockedSales();

    IEnumerable<Sale> GetAllExpiredOrders();

    IEnumerable<Sale> GetLastPaidSalesByClientAgreementId(long clientAgreementId, DateTime fromDate);

    IEnumerable<Sale> GetAllSalesByTaxFreePackListIdExceptProvided(long packListId, IEnumerable<long> ids);

    IEnumerable<Sale> GetAllSalesBySadIdExceptProvided(long sadId, IEnumerable<long> ids);

    long GetAllTotalAmount(SaleLifeCycleType? saleLifeCycleType, DateTime from, DateTime to);

    List<dynamic> GetSaleLifeCycleLine(Guid saleNetId);

    List<dynamic> GetTotalForSalesByYear(Guid clientNetId);

    dynamic GetSalesStatisticByDateRangeAndUserNetId(Guid netId, DateTime from, DateTime to);

    SaleStatisticsByManager GetSaleStatisticsByManagerRanged(long managerId, DateTime from, DateTime to);

    void SetBillDownloadDateByNetId(Guid netId);

    void SetNewUpdatedDate(Sale sale);
    void SetNewUpdatedDate(Guid netId);
    void Remove(Guid netId);

    TotalDashboardItem GetTotalAmountByDayAndCurrentMonth();

    void UpdateIsPrintedPaymentInvoice(long id);

    void UpdateIsAcceptedToPacking(long id, bool isAccepted);

    long AddCustomersOwnTtn(CustomersOwnTtn customersOwnTtn);

    void UpdateCustomersOwnTtn(CustomersOwnTtn customersOwnTtn);

    void RemoveCustomersOwnTtn(CustomersOwnTtn customersOwnTtn);

    CustomersOwnTtn GetCustomersOwnTtnById(long id);
}