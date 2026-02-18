using System;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class DeliveryExpense : EntityBase {
    public string InvoiceNumber { get; set; }

    public DateTime FromDate { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal VatPercent { get; set; }

    public decimal AccountingGrossAmount { get; set; }

    public decimal AccountingVatPercent { get; set; }

    public long SupplyOrderUkraineId { get; set; }

    public long SupplyOrganizationId { get; set; }

    public long SupplyOrganizationAgreementId { get; set; }

    public long? ConsumableProductId { get; set; }

    public long? ActProvidingServiceDocumentId { get; set; }

    public long? ActProvidingServiceId { get; set; }

    public long? AccountingActProvidingServiceId { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }

    public ActProvidingServiceDocument ActProvidingServiceDocument { get; set; }

    public ActProvidingService ActProvidingService { get; set; }

    public ActProvidingService AccountingActProvidingService { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public ConsumableProduct ConsumableProduct { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public decimal VatAmount { get; set; }
}