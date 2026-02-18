using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocol : EntityBase {
    public DeliveryProductProtocol() {
        SupplyInvoices = new HashSet<SupplyInvoice>();

        MergedServices = new HashSet<MergedService>();

        DeliveryProductProtocolDocuments = new HashSet<DeliveryProductProtocolDocument>();
    }

    public ICollection<SupplyInvoice> SupplyInvoices { get; set; }

    public SupplyTransportationType TransportationType { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public Organization Organization { get; set; }

    public long OrganizationId { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }

    public DateTime FromDate { get; set; }

    public string Comment { get; set; }

    public double Qty { get; set; }

    public decimal NetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsShipped { get; set; }

    public bool IsPlaced { get; set; }

    public bool IsPartiallyPlaced { get; set; }

    public long DeliveryProductProtocolNumberId { get; set; }

    public ICollection<DeliveryProductProtocolDocument> DeliveryProductProtocolDocuments { get; set; }

    public DeliveryProductProtocolNumber DeliveryProductProtocolNumber { get; set; }

    public int QtyInvoices { get; set; }

    public decimal TotalValue { get; set; }
}