using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IConsignmentsSyncRepository {
    IEnumerable<SyncOrganization> GetAllSyncOrganizations(string[] organziationNames);

    IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations();

    IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode);

    IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode);

    List<Organization> GetAllOrganizations();

    long Add(Organization organization);

    void Update(Organization organization);

    void Add(OrganizationTranslation translation);

    void Update(OrganizationTranslation translation);

    IEnumerable<SyncTaxInspection> GetAllSyncTaxInspections();

    IEnumerable<SyncTaxInspection> GetAmgAllSyncTaxInspections();

    List<TaxInspection> GetAllTaxInspections();

    long Add(TaxInspection taxInspection);

    void Add(OrderProductSpecification specification);

    void Update(TaxInspection taxInspection);

    IEnumerable<Currency> GetAllCurrencies();

    void Update(Currency currency);

    IEnumerable<SyncStorage> GetAllSyncStorages();

    IEnumerable<SyncStorage> GetAmgAllSyncStorages();

    List<Storage> GetAllStorages();

    long Add(Storage storage);

    void Update(Storage storage);

    IEnumerable<SyncStorage> GetSyncStoragesFromSyncConsignments();

    IEnumerable<SyncStorage> GetAmgSyncStoragesFromSyncConsignments();

    IEnumerable<string> GetStorageNamesFromSyncConsignmentsExceptProvided(IEnumerable<string> names);

    IEnumerable<string> GetAmgStorageNamesFromSyncConsignmentsExceptProvided(IEnumerable<string> names);

    IEnumerable<SyncConsignment> GetAllSyncConsignments(IEnumerable<string> storageNames);

    IEnumerable<SyncConsignment> GetAmgAllSyncConsignments(IEnumerable<string> storageNames);

    IEnumerable<SyncConsignment> GetAmgFilteredSyncOrderConsignments(SyncConsignmentDocumentInfo[] syncConsignmentDocuments);

    IEnumerable<SyncConsignment> GetFilteredSyncOrderConsignments(DateTime from, DateTime to);

    IEnumerable<SyncConsignmentDocumentInfo> GetSyncConsignmentDocumentInfos(DateTime from, DateTime to);

    IEnumerable<Client> GetAllClients();

    IEnumerable<Product> GetAllProductsByProductCodes(string inStatement, bool forAmg);

    long Add(ProductIncome productIncome);

    long Add(ProductCapitalization productCapitalization);

    long Add(ProductCapitalizationItem item);

    long Add(Consignment consignment);

    long Add(ProductIncomeItem item);

    long Add(ConsignmentItem consignmentItem);

    long Add(ProductSpecification productSpecification);

    void Add(ConsignmentItemMovement movement);

    long Add(SupplyOrderNumber supplyOrderNumber);

    long Add(SupplyProForm supplyProForm);

    long Add(SupplyOrder supplyOrder);

    long Add(SupplyInvoice supplyInvoice);

    long Add(PackingList packingList);

    long Add(PackingListPackageOrderItem packingListPackageOrderItem);

    long Add(ProductAvailability productAvailability);

    void Update(ProductAvailability productAvailability);

    ProductAvailability GetProductAvailabilityById(long id);

    void Add(ProductPlacement productPlacement);

    long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem);

    long Add(SupplyOrderItem supplyOrderItem);

    IEnumerable<Consignment> GetAllConsignmentsToDelete();

    void CleanAllConsignmentsToDelete();

    void Update(Consignment consignment);

    void Update(IEnumerable<Consignment> consignments);

    void Update(IEnumerable<ProductIncome> incomes);

    long Add(DeliveryProductProtocolNumber number);

    long Add(DeliveryProductProtocol deliveryProductProtocol);

    void Update(ConsignmentItem item);

    void RemoveConsignmentItemMovementsByItemId(long consignmentItemId);

    void RemoveProductCapitalizationById(long id);

    void RemoveProductCapitalizationItemById(long id);

    void RemovePackingListPackageOrderItemById(long id);

    void RemovePackingListById(long id);

    void RemoveSupplyInvoiceOrderItemById(long id);

    void RemoveSupplyInvoiceById(long id);

    void RemoveSupplyOrderItemById(long id);

    void RemoveSupplyOrderById(long id);

    void RemoveProductIncomeItemById(long id);

    void RemoveProductIncomeById(long id);

    IEnumerable<SyncConsignmentSpecification> GetAmgAllSyncConsignmentSpecifications(byte[] documentId);

    IEnumerable<SyncConsignmentSpecification> GetFenixAllSyncConsignmentSpecifications(
        byte[] documentId);

    void UpdateActiveSpecification();

    GovExchangeRate GetGovByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    IEnumerable<SupplyInvoice> GetSupplyInvoiceByIds(List<long> invoiceIds);

    IEnumerable<ConsignmentItem> GetConsignmentItemsByInvoiceIds(List<long> invoiceIds);

    void UpdatePrices(IEnumerable<ConsignmentItem> items);

    IEnumerable<SyncConsignmentSpend> GetAmgConsignmentSpendsByDocumentId(byte[] documentId);

    IEnumerable<SyncConsignmentSpend> GetFenixConsignmentSpendsByDocumentId(byte[] documentId);

    IEnumerable<SupplyOrganization> GetAllSupplyOrganizations(string name);

    SupplyOrganization GetDevSupplyOrganization(string name);

    ProductIncome GetLastProductIncome();

    long Add(SupplyOrganization supplyOrganization);

    long Add(SupplyOrganizationAgreement agreement);

    IEnumerable<SupplyInvoice> GetExistSupplyInvoices();

    SupplyInvoice GetExistSupplyInvoiceById(long id);

    IEnumerable<SupplyInvoice> GetExistSupplyInvoicesByIds(long[] ids);

    long Add(MergedService mergedService);

    void Add(SupplyInvoiceMergedService supplyInvoiceMergedService);

    long Add(ActProvidingService act);

    long Add(ConsumableProductCategory consumableProductCategory);

    long Add(ConsumableProduct consumableProduct);

    ConsumableProductCategory GetSupplyServiceConsumablesProductCategory();

    ConsumableProduct GetConsumablesProductByKey(string consignmentFromOneCUkKey);

    void UpdateInvoiceInProtocol(SupplyInvoice invoice);

    ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    Currency GetCurrencyByInvoice(long invoiceId);

    IEnumerable<SyncConsignment> GetAmgFilteredSyncCapitalizationConsignments(DateTime from, DateTime to);

    IEnumerable<SyncConsignment> GetFilteredSyncCapitalizationConsignments(DateTime from, DateTime to);

    IEnumerable<SyncConsignment> GetAmgFilteredSyncReturnConsignments(DateTime from, DateTime to);

    IEnumerable<SyncConsignment> GetFilteredSyncReturnConsignments(DateTime from, DateTime to);

    IEnumerable<PaymentMovement> GetAllPaymentMovementOperations();

    IEnumerable<string> GetAmgPaymentMovements();

    IEnumerable<string> GetFenixPaymentMovements();

    void AddPaymentMovementTranslation(PaymentMovementTranslation paymentMovementTranslation);

    long AddPaymentMovement(PaymentMovement paymentMovement);
}