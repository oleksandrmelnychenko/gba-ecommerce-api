using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyOrder : EntityBase {
    public SupplyOrder() {
        CustomServices = new HashSet<CustomService>();

        ResponsibilityDeliveryProtocols = new HashSet<ResponsibilityDeliveryProtocol>();

        PackingListDocuments = new HashSet<PackingListDocument>();

        InformationDeliveryProtocols = new HashSet<SupplyInformationDeliveryProtocol>();

        SupplyOrderPolandPaymentDeliveryProtocols = new HashSet<SupplyOrderPolandPaymentDeliveryProtocol>();

        SupplyOrderDeliveryDocuments = new HashSet<SupplyOrderDeliveryDocument>();

        SupplyInvoices = new HashSet<SupplyInvoice>();

        SupplyOrderItems = new HashSet<SupplyOrderItem>();

        CreditNoteDocuments = new HashSet<CreditNoteDocument>();

        SupplyOrderContainerServices = new HashSet<SupplyOrderContainerService>();

        SaleFutureReservations = new HashSet<SaleFutureReservation>();

        MergedServices = new HashSet<MergedService>();

        SupplyOrderVehicleServices = new HashSet<SupplyOrderVehicleService>();
    }

    public long ClientId { get; set; }
    public long? ResponsibleId { get; set; }

    public long ClientAgreementId { get; set; }

    public long OrganizationId { get; set; }

    public long SupplyOrderNumberId { get; set; }

    public long? SupplyProFormId { get; set; }

    public long? PortWorkServiceId { get; set; }

    public long? TransportationServiceId { get; set; }

    public long? CustomAgencyServiceId { get; set; }

    public long? PortCustomAgencyServiceId { get; set; }

    public long? PlaneDeliveryServiceId { get; set; }

    public long? VehicleDeliveryServiceId { get; set; }

    public long? AdditionalPaymentCurrencyId { get; set; }

    public string Comment { get; set; }

    public string InvoiceNumbers { get; set; }

    public string PackListNumbers { get; set; }

    public double Qty { get; set; }

    public decimal NetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AdditionalAmount { get; set; }

    public double AdditionalPercent { get; set; }

    public bool IsDocumentSet { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsOrderShipped { get; set; }

    public bool IsOrderArrived { get; set; }

    public bool IsPlaced { get; set; }

    public bool IsPartiallyPlaced { get; set; }

    public bool IsFullyPlaced { get; set; }

    public bool IsGrossPricesCalculated { get; set; }

    public bool IsOrderInsidePoland { get; set; }

    public bool IsApproved { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal TotalVat { get; set; }

    public decimal TotalQuantity { get; set; }

    public int TotalRowsQty { get; set; }

    public SupplyTransportationType TransportationType { get; set; }

    public DateTime? OrderShippedDate { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? CompleteDate { get; set; }

    public DateTime? ShipArrived { get; set; }

    public DateTime? VechicalArrived { get; set; }

    public DateTime? PlaneArrived { get; set; }

    public DateTime? OrderArrivedDate { get; set; }

    public DateTime? AdditionalPaymentFromDate { get; set; }

    public User Responsible { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public SupplyOrderNumber SupplyOrderNumber { get; set; }

    public SupplyProForm SupplyProForm { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public Currency AdditionalPaymentCurrency { get; set; }

    /// <summary>
    /// Ignored Entity for Totals
    /// </summary>
    public SupplyOrderTotals SupplyOrderTotals { get; set; }

    public ICollection<SupplyInvoice> SupplyInvoices { get; set; }

    public ICollection<CustomService> CustomServices { get; set; }

    public ICollection<ResponsibilityDeliveryProtocol> ResponsibilityDeliveryProtocols { get; set; }

    public ICollection<PackingListDocument> PackingListDocuments { get; set; }

    public ICollection<SupplyInformationDeliveryProtocol> InformationDeliveryProtocols { get; set; }

    public ICollection<SupplyOrderPolandPaymentDeliveryProtocol> SupplyOrderPolandPaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyOrderDeliveryDocument> SupplyOrderDeliveryDocuments { get; set; }

    public ICollection<SupplyOrderItem> SupplyOrderItems { get; set; }

    public ICollection<CreditNoteDocument> CreditNoteDocuments { get; set; }

    public ICollection<SupplyOrderContainerService> SupplyOrderContainerServices { get; set; }

    public ICollection<SupplyOrderVehicleService> SupplyOrderVehicleServices { get; set; }

    public ICollection<SaleFutureReservation> SaleFutureReservations { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }
}