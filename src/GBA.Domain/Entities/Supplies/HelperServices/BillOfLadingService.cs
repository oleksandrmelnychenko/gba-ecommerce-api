using System;
using System.Collections.Generic;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class BillOfLadingService : BaseService, IEquatable<BillOfLadingService> {
    public BillOfLadingService() {
        BillOfLadingDocuments = new HashSet<BillOfLadingDocument>();

        SupplyInvoiceBillOfLadingServices = new HashSet<SupplyInvoiceBillOfLadingService>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public DateTime? LoadDate { get; set; }

    public string BillOfLadingNumber { get; set; }

    public string TermDeliveryInDays { get; set; }

    public bool IsAutoCalculatedValue { get; set; }

    public bool IsShipped { get; set; }

    public long SupplyOrganizationId { get; set; }

    public SupplyExtraChargeType SupplyExtraChargeType { get; set; }

    public bool IsCalculatedValue { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public long DeliveryProductProtocolId { get; set; }

    public DeliveryProductProtocol DeliveryProductProtocol { get; set; }

    public ICollection<BillOfLadingDocument> BillOfLadingDocuments { get; set; }

    public ICollection<SupplyInvoiceBillOfLadingService> SupplyInvoiceBillOfLadingServices { get; set; }

    public TypeBillOfLadingService TypeBillOfLadingService { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }

    public bool Equals(BillOfLadingService other) {
        if (other == null) return false;

        if (!SupplyOrganizationId.Equals(other.SupplyOrganizationId))
            return false;
        if (!SupplyOrganizationAgreementId.Equals(other.SupplyOrganizationAgreementId))
            return false;
        if (!NetPrice.Equals(other.NetPrice))
            return false;
        if (!AccountingNetPrice.Equals(other.AccountingNetPrice))
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