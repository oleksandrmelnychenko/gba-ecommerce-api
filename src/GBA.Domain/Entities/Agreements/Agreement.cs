using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Entities.Agreements;

public sealed class Agreement : EntityBase {
    public Agreement() {
        ClientAgreements = new HashSet<ClientAgreement>();

        ClientInDebts = new HashSet<ClientInDebt>();
    }

    public string Name { get; set; }

    public string DeferredPayment { get; set; }

    public string TermsOfPayment { get; set; }

    public string Number { get; set; }

    public bool IsManagementAccounting { get; set; }

    public bool IsAccounting { get; set; }

    public bool IsSelected { get; set; }

    public bool WithVATAccounting { get; set; }

    public bool IsControlAmountDebt { get; set; }

    public bool IsControlNumberDaysDebt { get; set; }

    public bool IsPrePaymentFull { get; set; }

    public bool IsPrePayment { get; set; }

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public decimal AmountDebt { get; set; }

    public double PrePaymentPercentages { get; set; }

    public int NumberDaysDebt { get; set; }

    public long? CurrencyId { get; set; }

    public long? OrganizationId { get; set; }

    public long? PricingId { get; set; }

    public long? ProviderPricingId { get; set; }

    public long? TaxAccountingSchemeId { get; set; }

    public long? AgreementTypeCivilCodeId { get; set; }

    public long? PromotionalPricingId { get; set; }

    public bool ForReSale { get; set; }

    public bool WithAgreementLine { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public bool IsDefaultForSyncConsignment { get; set; }

    public ProviderPricing ProviderPricing { get; set; }

    public Pricing Pricing { get; set; }

    public Pricing PromotionalPricing { get; set; }

    public Organization Organization { get; set; }

    public Currency Currency { get; set; }

    public TaxAccountingScheme TaxAccountingScheme { get; set; }

    public AgreementTypeCivilCode AgreementTypeCivilCode { get; set; }

    public ICollection<ClientAgreement> ClientAgreements { get; set; }

    public ICollection<ClientInDebt> ClientInDebts { get; set; }

    public int ExpiredDays { get; set; }

    public bool HasPromotionalPricing { get; set; }

    public bool SourceIdsEqual(byte[] sourceId) {
        return SourceAmgId != null && sourceId != null && SourceAmgId.SequenceEqual(sourceId) ||
               SourceFenixId != null && sourceId != null && SourceFenixId.SequenceEqual(sourceId);
    }
}