using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class Currency : EntityBase {
    public Currency() {
        Agreements = new HashSet<Agreement>();

        Pricings = new HashSet<Pricing>();

        ProviderPricings = new HashSet<ProviderPricing>();

        CurrencyTranslations = new HashSet<CurrencyTranslation>();

        ExchangeRates = new HashSet<ExchangeRate>();

        FromCrossExchangeRates = new HashSet<CrossExchangeRate>();

        ToCrossExchangeRates = new HashSet<CrossExchangeRate>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        PaymentCurrencyRegisters = new HashSet<PaymentCurrencyRegister>();

        SupplyOrganizationAgreements = new HashSet<SupplyOrganizationAgreement>();

        OrganizationClientAgreements = new HashSet<OrganizationClientAgreement>();

        Organizations = new HashSet<Organization>();

        SupplyOrders = new HashSet<SupplyOrder>();

        SupplyOrderUkraines = new HashSet<SupplyOrderUkraine>();

        GovExchangeRates = new HashSet<GovExchangeRate>();

        FromGovCrossExchangeRates = new HashSet<GovCrossExchangeRate>();

        ToGovCrossExchangeRates = new HashSet<GovCrossExchangeRate>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public string Name { get; set; }

    public string Code { get; set; }

    public string CodeOneC { get; set; }

    public ICollection<ExchangeRate> ExchangeRates { get; set; }

    public ICollection<CrossExchangeRate> FromCrossExchangeRates { get; set; }

    public ICollection<CrossExchangeRate> ToCrossExchangeRates { get; set; }

    public ICollection<Agreement> Agreements { get; set; }

    public ICollection<Pricing> Pricings { get; set; }

    public ICollection<ProviderPricing> ProviderPricings { get; set; }

    public ICollection<CurrencyTranslation> CurrencyTranslations { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<PaymentCurrencyRegister> PaymentCurrencyRegisters { get; set; }

    public ICollection<SupplyOrganizationAgreement> SupplyOrganizationAgreements { get; set; }

    public ICollection<OrganizationClientAgreement> OrganizationClientAgreements { get; set; }

    public ICollection<Organization> Organizations { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<SupplyOrderUkraine> SupplyOrderUkraines { get; set; }

    public ICollection<GovExchangeRate> GovExchangeRates { get; set; }

    public ICollection<GovCrossExchangeRate> FromGovCrossExchangeRates { get; set; }

    public ICollection<GovCrossExchangeRate> ToGovCrossExchangeRates { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}