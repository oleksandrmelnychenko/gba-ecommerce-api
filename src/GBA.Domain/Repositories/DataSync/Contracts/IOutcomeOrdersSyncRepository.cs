using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IOutcomeOrdersSyncRepository {
    IEnumerable<SyncOrganization> GetAllSyncOrganizations();

    IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations();

    List<Organization> GetAllOrganizations();

    long Add(Organization organization);

    void Update(Organization organization);

    void Add(OrganizationTranslation translation);

    void Update(OrganizationTranslation translation);

    IEnumerable<SyncTaxInspection> GetAllSyncTaxInspections();

    IEnumerable<SyncTaxInspection> GetAmgAllSyncTaxInspections();

    List<TaxInspection> GetAllTaxInspections();

    long Add(TaxInspection taxInspection);

    void Update(TaxInspection taxInspection);

    IEnumerable<Currency> GetAllCurrencies();

    void Update(Currency currency);

    IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode);

    IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode);

    IEnumerable<ClientAgreement> GetAllClientAgreementsToSync();

    IEnumerable<SyncSettlement> GetSyncSettlements(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName);

    IEnumerable<SyncSettlement> GetAmgSyncSettlements(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName);

    IEnumerable<SyncSaleReturnItem> GetSaleReturnItemsBySourceId(byte[] sourceId);

    IEnumerable<SyncSaleReturnItem> GetAmgSaleReturnItemsBySourceId(byte[] sourceId);

    IEnumerable<SyncSaleItem> GetSaleItemsBySourceId(byte[] sourceId);

    IEnumerable<SyncSaleItem> GetAmgSaleItemsBySourceId(byte[] sourceId);

    IEnumerable<SyncIncomePaymentOrderSale> GetAllIncomePaymentOrderSalesBySourceId(byte[] sourceId, bool cashOrder);

    IEnumerable<SyncIncomePaymentOrderSale> GetAllAmgIncomePaymentOrderSalesBySourceId(byte[] sourceId);

    IEnumerable<SyncProductTransferItem> GetAllAmgProductTransferItems(DateTime from, DateTime to);

    IEnumerable<SyncProductTransferItem> GetAllProductTransferItems(DateTime from, DateTime to);

    IEnumerable<SyncProductTransferItem> GetAllActProductTransferItems(DateTime from, DateTime to);

    IEnumerable<SyncDepreciatedOrderItem> GetAllDepreciatedOrderItems(DateTime from, DateTime to);

    SyncIncomePaymentOrder GetIncomePaymentOrderBySourceId(byte[] sourceId);

    SyncIncomePaymentOrder GetAmgIncomePaymentOrderBySourceId(byte[] sourceId);

    SyncIncomeCashOrder GetIncomeCashOrderBySourceId(byte[] sourceId);

    SyncIncomeCashOrder GetAmgIncomeCashOrderBySourceId(byte[] sourceId);

    SyncOutcomePaymentOrder GetOutcomePaymentOrderBySourceId(byte[] sourceId);

    SyncOutcomePaymentOrder GetAmgOutcomePaymentOrderBySourceId(byte[] sourceId);

    long Add(Order entity);

    long Add(BaseLifeCycleStatus status);

    long Add(BaseSalePaymentStatus status);

    long Add(SaleNumber number);

    long Add(DeliveryRecipient deliveryRecipient);

    long Add(DeliveryRecipientAddress address);

    long Add(Debt entity);

    void Add(ClientInDebt entity);

    long Add(Sale sale);

    long AddDeliveryRecipient(DeliveryRecipient deliveryRecipient);

    long AddDeliverRecipientAddress(DeliveryRecipientAddress deliveryRecipientAddress);

    void Add(OrderItem orderItem);

    Product GetProductBySourceCodeWithIncludes(long sourceCode, bool forAmg);

    Product GetProductBySourceCode(long sourceCode, bool forAmg);

    OrderItem GetOrderItemBySaleNumberAndProductCode(string saleNumber, long productCode);

    long Add(SaleReturn saleReturn);

    void Add(SaleReturnItem saleReturnItem);

    OrderItem GetLastOrderItemByClientAgreementAndProductIdsIfExists(long clientAgreementId, long productId);

    Order GetLastOrderByClientAgreementId(long clientAgreementId);

    Sale GetSaleByOrderItemId(long orderItemId);

    Sale GetSaleIfExists(long clientAgreementId, string number, DateTime fromDate);

    long AddWithId(OrderItem orderItem);

    Storage GetStorageIfExists();

    Storage GetStorageByName(string name);

    long Add(Storage storage);

    Organization GetOrganizationByName(string name);

    PaymentRegister GetPaymentRegister(string name, string currencyOneCCode);

    long Add(OutcomePaymentOrder outcomePaymentOrder);

    void Add(PaymentMovementOperation paymentMovementOperation);

    long Add(PaymentMovement paymentMovement);

    PaymentMovement GetPaymentMovementByName(string name);

    IEnumerable<SyncOrderItem> GetAllSyncOrderItems(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName);

    IEnumerable<SyncOrderItem> GetAmgAllSyncOrderItems(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName);

    long Add(IncomePaymentOrder incomePaymentOrder);

    decimal GetExchangeRateAmountToEuroByDate(long fromCurrencyId, DateTime fromDate);

    void CleanDebtsAndBalances();

    IEnumerable<SyncAccounting> GetSyncAccountingFiltered(long clientCode, string agreementName, string organizationName);

    IEnumerable<SyncAccounting> GetAmgSyncAccountingFiltered(long clientCode, string agreementName, string organizationName);

    void Update(ClientAgreement agreement);

    Product GetDevProduct();

    MeasureUnit GetMeasureUnit();

    long Add(MeasureUnit measureUnit);

    void Add(MeasureUnitTranslation translation);

    long Add(Product product);

    IEnumerable<ProductAvailability> GetAvailabilities(
        long productId,
        long organizationId,
        bool vatStoragesFirst);

    ProductAvailability GetAvailability(
        long productId,
        long storageId);

    void Add(ProductAvailability availability);

    void Update(ProductAvailability availability);

    void Add(ProductReservation reservation);

    IEnumerable<SyncOrderSaleItem> GetAmgFilteredSyncOrderSaleItems(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncOrderSaleItem> GetFilteredSyncOrderSaleItems(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<Product> GetProductByCodes(long[] productCodes, bool forAmg);

    IEnumerable<Storage> GetAllStorages();

    IEnumerable<SupplyOrganization> GetSupplyOrganizationWithData(long[] supplierCodes, bool forAmg);

    IEnumerable<Client> GetClientsWithData(long[] clientCodes, bool forAmg);

    IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncIncomeCashOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncIncomeCashOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncIncomeBankOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncIncomeBankOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncOutcomeCashBankOrder> GetAmgFilteredSyncOutcomeCashOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncOutcomeCashBankOrder> GetFilteredSyncOutcomeCashOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncOutcomeCashBankOrder> GetAmgFilteredSyncOutcomeBankOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncOutcomeCashBankOrder> GetFilteredSyncOutcomeBankOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<PaymentMovement> GetAllPaymentMovementOperations();

    void AddPaymentMovementTranslation(PaymentMovementTranslation paymentMovementTranslation);

    long AddPaymentMovement(PaymentMovement paymentMovement);

    IEnumerable<Client> GetDeletedClients();

    IEnumerable<Client> GetClientsByIds(long[] mainClientIds);

    IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncInternalMovementCashOrders(
        DateTime fromDate,
        DateTime toDate);

    IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncInternalMovementCashOrders(
        DateTime fromDate,
        DateTime toDate);
}