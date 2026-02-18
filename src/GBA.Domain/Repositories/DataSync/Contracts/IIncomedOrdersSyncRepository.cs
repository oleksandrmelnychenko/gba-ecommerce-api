using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IIncomedOrdersSyncRepository {
    IEnumerable<SyncOrganization> GetAllSyncOrganizations(string[] organizationNames);

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

    IEnumerable<SyncStorage> GetAllSyncStorages();

    IEnumerable<SyncStorage> GetAmgAllSyncStorages();

    List<Storage> GetAllStorages();

    long Add(Storage storage);

    void Update(Storage storage);

    IEnumerable<string> GetStorageNamesFromSyncConsignmentsExceptProvided(
        IEnumerable<string> names,
        DateTime fromDate);


    IEnumerable<string> GetAmgStorageNamesFromSyncConsignmentsExceptProvided(
        IEnumerable<string> names,
        DateTime fromDate);

    IEnumerable<SyncConsignmentSpecification> GetAllSyncConsignmentSpecifications();

    IEnumerable<Client> GetAllClients();

    IEnumerable<Product> GetAllProductsByProductCodes(string inStatement, bool forAmg);

    long Add(ProductIncome productIncome);

    long Add(ProductCapitalization productCapitalization);

    long Add(ProductCapitalizationItem item);

    long Add(Consignment consignment);

    void Update(Consignment consignment);

    long Add(ProductIncomeItem item);

    long Add(ConsignmentItem consignmentItem);

    void Update(ConsignmentItem item);

    long Add(ProductSpecification productSpecification);

    void RemoveConsignmentItemMovementsByItemId(long consignmentItemId);

    void Add(ConsignmentItemMovement movement);

    long Add(SupplyOrderNumber supplyOrderNumber);

    long Add(SupplyProForm supplyProForm);

    long Add(SupplyOrder supplyOrder);

    long Add(SupplyInvoice supplyInvoice);

    long Add(PackingList packingList);

    long Add(PackingListPackageOrderItem packingListPackageOrderItem);

    long Add(ProductAvailability productAvailability);

    void Update(ProductAvailability productAvailability);

    void Add(ProductPlacement productPlacement);

    long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem);

    long Add(SupplyOrderItem supplyOrderItem);

    void UpdateActiveSpecification();

    IEnumerable<SyncConsignment> GetAllSyncConsignments(IEnumerable<string> storageNames, DateTime fromDate);

    IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode);

    IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode);

    IEnumerable<SyncConsignment> GetAmgAllSyncConsignments(IEnumerable<string> storages, DateTime operationCreated);

    void UpdatePrices(IEnumerable<ConsignmentItem> items);

    IEnumerable<ConsignmentItem> GetConsignmentItemsByInvoiceIds(List<long> invoiceIds);

    IEnumerable<SupplyInvoice> GetSupplyInvoiceByIds(List<long> invoiceIds);

    GovExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate);

    IEnumerable<SyncConsignmentSpend> GetFenixConsignmentSpendsByDocumentId(byte[] documentId);

    IEnumerable<SyncConsignmentSpend> GetAmgConsignmentSpendsByDocumentId(byte[] documentId);

    IEnumerable<SyncConsignmentSpecification> GetFenixAllSyncConsignmentSpecifications(
        byte[] documentId);

    IEnumerable<SyncConsignmentSpecification> GetAmgAllSyncConsignmentSpecifications(
        byte[] documentId);

    SupplyOrganization GetDevSupplyOrganization(string name);

    IEnumerable<SupplyOrganization> GetAllSupplyOrganizations(string defaultComment);

    IEnumerable<Consignment> GetAllConsignmentsToDelete();

    ProductAvailability GetProductAvailabilityById(long id);

    void CleanAllConsignmentsToDelete();

    long Add(SupplyOrganizationAgreement agreement);

    long Add(SupplyOrganization supplyOrganization);
}