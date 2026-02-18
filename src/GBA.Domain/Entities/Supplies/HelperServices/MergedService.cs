using System;
using System.Collections.Generic;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class MergedService : BaseService, IEquatable<MergedService> {
    public MergedService() {
        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        InvoiceDocuments = new HashSet<InvoiceDocument>();

        SupplyInvoiceMergedServices = new HashSet<SupplyInvoiceMergedService>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public long SupplyOrganizationId { get; set; }

    public long? SupplyOrderId { get; set; }

    public long? SupplyOrderUkraineId { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public SupplyExtraChargeType SupplyExtraChargeType { get; set; }

    public bool IsCalculatedValue { get; set; }

    public bool IsAutoCalculatedValue { get; set; }

    public long? ConsumableProductId { get; set; }

    public ConsumableProduct ConsumableProduct { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public long? DeliveryProductProtocolId { get; set; }

    public DeliveryProductProtocol DeliveryProductProtocol { get; set; }

    public ICollection<SupplyInvoiceMergedService> SupplyInvoiceMergedServices { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }

    public bool Equals(MergedService other) {
        if (other == null) return false;

        if (!SupplyOrganizationId.Equals(other.SupplyOrganizationId))
            return false;
        if (!SupplyOrganizationAgreementId.Equals(other.SupplyOrganizationAgreementId))
            return false;
        if (!GrossPrice.Equals(other.GrossPrice))
            return false;
        if (!AccountingGrossPrice.Equals(other.AccountingGrossPrice))
            return false;
        if (!VatPercent.Equals(other.VatPercent))
            return false;
        if (!AccountingVatPercent.Equals(other.AccountingVatPercent))
            return false;
        if (FromDate.HasValue && other.FromDate.HasValue)
            return FromDate.Value.DateTimeEqualExtension(other.FromDate.Value);
        return true;
    }
}