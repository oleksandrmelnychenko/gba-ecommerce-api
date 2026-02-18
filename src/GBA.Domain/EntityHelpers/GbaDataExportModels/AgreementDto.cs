using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public class AgreementDto {
    public Guid NetUid { get; set; }
    public string Name { get; set; }
    public DateTime Created { get; set; }
    public string Number { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class ExtendedAgreementGTDDto {
    public Guid NetUid { get; set; }
    public string Name { get; set; }
    public string Number { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public CurrencyDto CurrencyDocument { get; set; }
}
public class ExtendedAgreementDto : AgreementDto {
    public string DeferredPayment { get; set; }

    public string TermsOfPayment { get; set; }

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

    public bool IsDefaultForSyncConsignment { get; set; }

    public bool Foreign { get; set; }

    public ProviderPricing ProviderPricing { get; set; }

    public Pricing Pricing { get; set; }

    public Pricing PromotionalPricing { get; set; }

    public Organization Organization { get; set; }

    public CurrencyDto Currency { get; set; }

    public TaxAccountingScheme TaxAccountingScheme { get; set; }

    public AgreementTypeCivilCode AgreementTypeCivilCode { get; set; }
}