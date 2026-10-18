using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class Organization : EntityBase {
    public Organization() {
        Storages = new HashSet<Storage>();

        Agreements = new HashSet<Agreement>();

        OrganizationTranslations = new HashSet<OrganizationTranslation>();

        SaleNumbers = new HashSet<SaleNumber>();

        SupplyOrders = new HashSet<SupplyOrder>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        PaymentRegisters = new HashSet<PaymentRegister>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        ConsumablesStorages = new HashSet<ConsumablesStorage>();

        CompanyCars = new HashSet<CompanyCar>();

        SupplyOrganizationAgreements = new HashSet<SupplyOrganizationAgreement>();

        SupplyOrderUkraines = new HashSet<SupplyOrderUkraine>();

        DepreciatedOrders = new HashSet<DepreciatedOrder>();

        ProductTransfers = new HashSet<ProductTransfer>();

        SupplyReturns = new HashSet<SupplyReturn>();

        TaxFreePackLists = new HashSet<TaxFreePackList>();

        Sads = new HashSet<Sad>();

        ProductCapitalizations = new HashSet<ProductCapitalization>();

        AdvancePayments = new HashSet<AdvancePayment>();

        Consignments = new HashSet<Consignment>();

        DeliveryProductProtocols = new List<DeliveryProductProtocol>();

        ReSales = new HashSet<ReSale>();
    }

    public string Name { get; set; }

    public string NameUk { get; set; }

    public string NamePl { get; set; }

    public string FullName { get; set; }

    /// <summary>
    /// Taxpayer Identification Number.
    /// </summary>
    public string TIN { get; set; }

    /// <summary>
    /// Unified State Register of Enterprises and Organizations of Ukraine.
    /// </summary>
    public string USREOU { get; set; }

    /// <summary>
    /// State register of individuals.
    /// </summary>
    public string SROI { get; set; }

    public string Code { get; set; }

    public string Culture { get; set; }

    public string RegistrationNumber { get; set; }

    public string PFURegistrationNumber { get; set; }

    public string PhoneNumber { get; set; }

    public string Address { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public DateTime? PFURegistrationDate { get; set; }

    public bool IsIndividual { get; set; }

    public long? CurrencyId { get; set; }

    public long? StorageId { get; set; }

    public long? TaxInspectionId { get; set; }

    public string Manager { get; set; }

    public TypeTaxation TypeTaxation { get; set; }

    public long? VatRateId { get; set; }

    public bool IsVatAgreements { get; set; }

    public VatRate VatRate { get; set; }

    public Currency Currency { get; set; }

    public Storage Storage { get; set; }

    public TaxInspection TaxInspection { get; set; }

    public ICollection<Storage> Storages { get; set; }

    public ICollection<Agreement> Agreements { get; set; }

    public ICollection<OrganizationTranslation> OrganizationTranslations { get; set; }

    public ICollection<SaleNumber> SaleNumbers { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<PaymentRegister> PaymentRegisters { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<ConsumablesStorage> ConsumablesStorages { get; set; }

    public ICollection<CompanyCar> CompanyCars { get; set; }

    public ICollection<SupplyOrganizationAgreement> SupplyOrganizationAgreements { get; set; }

    public ICollection<SupplyOrderUkraine> SupplyOrderUkraines { get; set; }

    public ICollection<DepreciatedOrder> DepreciatedOrders { get; set; }

    public ICollection<ProductTransfer> ProductTransfers { get; set; }

    public ICollection<SupplyReturn> SupplyReturns { get; set; }

    public ICollection<TaxFreePackList> TaxFreePackLists { get; set; }

    public ICollection<Sad> Sads { get; set; }

    public ICollection<ProductCapitalization> ProductCapitalizations { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }

    public ICollection<Consignment> Consignments { get; set; }

    public ICollection<DeliveryProductProtocol> DeliveryProductProtocols { get; set; }

    public ICollection<ReSale> ReSales { get; set; }

    public PaymentRegister MainPaymentRegister { get; set; }
}