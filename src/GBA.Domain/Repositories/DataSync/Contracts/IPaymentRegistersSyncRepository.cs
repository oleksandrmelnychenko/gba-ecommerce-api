using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IPaymentRegistersSyncRepository {
    IEnumerable<SyncOrganization> GetAllSyncOrganizations(string[] organizationNames);

    IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations();

    IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode);

    IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode);

    List<Organization> GetAllOrganizations();

    long Add(Organization organization);

    void Update(Organization organization);

    void Add(OrganizationTranslation translation);

    void Update(OrganizationTranslation translation);

    IEnumerable<Currency> GetAllCurrencies();

    void Update(Currency currency);

    IEnumerable<SyncTaxInspection> GetAllSyncTaxInspections();

    IEnumerable<SyncTaxInspection> GetAmgAllSyncTaxInspections();

    List<TaxInspection> GetAllTaxInspections();

    long Add(TaxInspection taxInspection);

    void Update(TaxInspection taxInspection);

    IEnumerable<SyncCashRegister> GetAllSyncCashRegisters(string[] organization);

    IEnumerable<SyncCashRegister> GetAmgAllSyncCashRegisters(string organization);

    List<PaymentRegister> GetAllPaymentRegisters();

    long Add(PaymentRegister register);

    void Update(PaymentRegister register);

    long Add(PaymentCurrencyRegister register);

    void Update(PaymentCurrencyRegister register);

    IEnumerable<SyncBankRegister> GetAllSyncBankRegisters(string[] organization);

    IEnumerable<SyncBankRegister> GetAmgAllSyncBankRegisters(string organization);

    IEnumerable<SyncBank> GetAmgAllSyncBanks();

    IEnumerable<SyncBank> GetAllSyncBanks();

    List<Bank> GetAllBanks();

    long Add(Bank bank);

    void Update(Bank bank);

    List<Storage> GetAllStorages();

    IEnumerable<VatRate> GetAllVatRates();

    long AddVatRate(VatRate vatRate);
}