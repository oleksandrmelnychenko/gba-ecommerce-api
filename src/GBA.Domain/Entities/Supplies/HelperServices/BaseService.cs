using System;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public abstract class BaseService : EntityBase {
    public bool IsActive { get; set; }

    public DateTime? FromDate { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal NetPrice { get; set; }

    public decimal Vat { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public decimal AccountingSupplyCostsWithinCountry { get; set; }

    public decimal AccountingNetPrice { get; set; }

    public decimal AccountingVat { get; set; }

    public double VatPercent { get; set; }

    public double AccountingVatPercent { get; set; }

    public string Number { get; set; }

    public string ServiceNumber { get; set; }

    public string Name { get; set; }

    public long? UserId { get; set; }

    public long? SupplyPaymentTaskId { get; set; }

    public long? AccountingPaymentTaskId { get; set; }

    public long SupplyOrganizationAgreementId { get; set; }

    public long? SupplyInformationTaskId { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? AccountingExchangeRate { get; set; }

    public bool IsIncludeAccountingValue { get; set; }

    public long? ActProvidingServiceDocumentId { get; set; }

    public long? SupplyServiceAccountDocumentId { get; set; }

    public long? ActProvidingServiceId { get; set; }

    public long? AccountingActProvidingServiceId { get; set; }

    public ActProvidingService ActProvidingService { get; set; }

    public ActProvidingService AccountingActProvidingService { get; set; }

    public SupplyServiceAccountDocument SupplyServiceAccountDocument { get; set; }

    public ActProvidingServiceDocument ActProvidingServiceDocument { get; set; }

    public virtual SupplyInformationTask SupplyInformationTask { get; set; }

    public virtual User User { get; set; }

    public virtual SupplyPaymentTask SupplyPaymentTask { get; set; }

    public virtual SupplyPaymentTask AccountingPaymentTask { get; set; }

    public virtual SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }
}