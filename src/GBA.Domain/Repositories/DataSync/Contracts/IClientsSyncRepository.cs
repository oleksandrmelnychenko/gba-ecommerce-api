using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IClientsSyncRepository {
    IEnumerable<SyncClient> GetAllSyncClients();

    IEnumerable<SyncClient> GetAmgAllSyncClients();

    IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode);

    IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode);

    List<Client> GetAllClients();

    IEnumerable<Region> GetAllRegions();

    long Add(RegionCode regionCode);

    void Update(RegionCode regionCode);

    long Add(Client client);

    void Update(Client client);

    void Add(ClientInRole clientInRole);

    void Update(ClientInRole clientInRole);

    void RemoveSubClientsByRootClientId(long rootClientId);

    void Add(ClientSubClient clientSubClient);

    long Add(ClientBankDetailAccountNumber accountNumber);

    long Add(ClientBankDetailIbanNo ibanNo);

    long Add(ClientBankDetails bankDetails);

    IEnumerable<SyncOrganization> GetAllSyncOrganizations(string[] organizations);

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

    IEnumerable<SyncCurrency> GetAmgAllSyncCurrencies();

    IEnumerable<SyncCurrency> GetAllSyncCurrencies();

    void Update(Currency currency);

    void SetSharesPricings();

    IEnumerable<SyncPricing> GetAllSyncPricings();

    IEnumerable<SyncPricing> GetAmgAllSyncPricings();

    List<Pricing> GetAllPricings();

    long Add(Pricing pricing);

    void Update(Pricing pricing);

    void Add(PricingTranslation translation);

    IEnumerable<SyncAgreement> GetAllSyncAgreementsByCode(
        long supplierCode,
        bool? isProvider = null);

    IEnumerable<SyncAgreement> GetAmgAllSyncAgreementsByCode(
        long supplierCode);

    void RemoveAllAgreementsByClientId(long clientId);

    long Add(Agreement agreement);

    long Add(ClientAgreement clientAgreement);

    void Update(Agreement agreement);

    void Update(ClientAgreement clientAgreement);

    long Add(ProviderPricing pricing);

    long Add(Currency currency);

    Pricing GetPricingByCultureWithHighestExtraCharge(string culture);

    Organization GetOrganizationByCultureIfExists(string culture);

    IEnumerable<SyncStorage> GetAllSyncStorages();

    IEnumerable<SyncStorage> GetAmgAllSyncStorages();

    List<Storage> GetAllStorages();

    long Add(Storage storage);

    void Update(Storage storage);

    void AddDefaultDiscountsForSpecificClientAgreement(long clientAgreementId);

    IEnumerable<ProductGroup> GetAllProductGroups();

    IEnumerable<SyncDiscount> GetAllDiscountsForSpecificClient(long clientCode, string pricingName);

    IEnumerable<SyncDiscount> GetAmgAllDiscountsForSpecificClient(long clientCode, string pricingName);

    void ExecuteSql(string sqlExpression);

    IEnumerable<SyncDeliveryRecipient> GetAllSyncDeliveryRecipientsByClientCode(long clientCode);

    IEnumerable<SyncDeliveryRecipient> GetAmgAllSyncDeliveryRecipientsByClientCode(long clientCode);

    List<DeliveryRecipient> GetAllDeliveryRecipientsByClientId(long clientId);

    IEnumerable<Client> GetAllClientsWithRegionCodes();

    IEnumerable<Client> GetAllBySearchPatterns(string pattern, string additionalPattern, long exceptClientId);

    long Add(DeliveryRecipient recipient);

    void Add(DeliveryRecipientAddress address);

    void ReAssignClientAgreements(long fromClientId, long toClientId, bool forAmg);

    void RemoveClient(long clientId, long mainClientId);

    ClientSubClient GetClientSubClientIfExists(long rootClientId, long subClientId);

    void Update(ClientSubClient subClient);

    int GetNameDistance(string name1, string name2);

    ClientAgreement GetClientAgreementBySourceId(byte[] sourceId, bool forAmg);

    ProductGroupDiscount GetProductGroupDiscountByClientAgreementIdAndProductGroupId(
        long clientAgreementId,
        long productGroupId);

    List<SupplyOrganization> GetAllSupplyOrganization();

    long Add(SupplyOrganization supplyOrganization);

    void Update(SupplyOrganization supplyOrganization);

    long Add(SupplyOrganizationAgreement agreement);

    void Update(SupplyOrganizationAgreement agreement);

    void AddClientUserProfile(ClientUserProfile clientUserProfile);

    Currency GetEURCurrencyIfExists();

    Currency GetPLNCurrencyIfExists();

    Currency GetUAHCurrencyIfExists();

    Currency GetUSDCurrencyIfExists();

    IEnumerable<ClientType> GetAllClientTypes();

    IEnumerable<ClientTypeRole> GetAllClientTypeRoles();

    IEnumerable<PriceType> GetAllPriceTypes();

    void DeleteDefaultAgreementForSyncConsignments();

    void DeleteClientByNetId(Guid netId);

    void DeleteClientById(long id);

    IEnumerable<VatRate> GetAllVatRates();

    long AddVatRate(VatRate vatRate);
}