using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyInvoice : EntityBase {
    public SupplyInvoice() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        PaymentDeliveryProtocols = new HashSet<SupplyOrderPaymentDeliveryProtocol>();

        InformationDeliveryProtocols = new HashSet<SupplyInformationDeliveryProtocol>();

        PackingLists = new HashSet<PackingList>();

        SupplyInvoiceOrderItems = new HashSet<SupplyInvoiceOrderItem>();

        ActReconciliations = new HashSet<ActReconciliation>();

        OrderProductSpecifications = new HashSet<OrderProductSpecification>();

        SupplyInvoiceMergedServices = new HashSet<SupplyInvoiceMergedService>();

        SupplyInvoiceBillOfLadingServices = new HashSet<SupplyInvoiceBillOfLadingService>();

        SupplyInvoiceDeliveryDocuments = new HashSet<SupplyInvoiceDeliveryDocument>();

        MergedSupplyInvoices = new HashSet<SupplyInvoice>();
    }

    public string Number { get; set; }

    public string ServiceNumber { get; set; }

    public string Comment { get; set; }

    public decimal NetPrice { get; set; }

    public decimal ExtraCharge { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal TotalNetPriceWithVat { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal AccountingTotalGrossPrice { get; set; }

    public decimal TotalSpending { get; set; }

    public decimal AccountingTotalSpending { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal ExchangeRateEurToUah { get; set; }

    public bool IsShipped { get; set; }

    public bool IsPartiallyPlaced { get; set; }

    public bool IsFullyPlaced { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? PaymentTo { get; set; }

    public long SupplyOrderId { get; set; }

    public long? SupplyOrganizationAgreementId { get; set; }

    public long? SupplyOrganizationId { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalCBM { get; set; }

    public double TotalGrossWeight { get; set; }

    public double TotalQuantity { get; set; }

    public string NumberCustomDeclaration { get; set; }

    public DateTime? DateCustomDeclaration { get; set; }

    public decimal DeliveryAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalValueWithVat { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<SupplyOrderPaymentDeliveryProtocol> PaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyInformationDeliveryProtocol> InformationDeliveryProtocols { get; set; }

    public ICollection<PackingList> PackingLists { get; set; }

    public ICollection<SupplyInvoiceOrderItem> SupplyInvoiceOrderItems { get; set; }

    public ICollection<ActReconciliation> ActReconciliations { get; set; }

    public ICollection<OrderProductSpecification> OrderProductSpecifications { get; set; }

    public long? DeliveryProductProtocolId { get; set; }

    public long? RootSupplyInvoiceId { get; set; }

    public DeliveryProductProtocol DeliveryProductProtocol { get; set; }

    public SupplyInvoice RootSupplyInvoice { get; set; }

    public ICollection<SupplyInvoiceMergedService> SupplyInvoiceMergedServices { get; set; }

    public ICollection<SupplyInvoiceBillOfLadingService> SupplyInvoiceBillOfLadingServices { get; set; }

    public ICollection<SupplyInvoiceDeliveryDocument> SupplyInvoiceDeliveryDocuments { get; set; }

    public ICollection<SupplyInvoice> MergedSupplyInvoices { get; set; }
}