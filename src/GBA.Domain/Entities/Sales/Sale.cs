using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.NumeratorMessages;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Entities.UserNotifications;

namespace GBA.Domain.Entities.Sales;

public sealed class Sale : EntityBase {
    public Sale() {
        SaleExchangeRates = new HashSet<SaleExchangeRate>();

        HistoryInvoiceEdit = new HashSet<HistoryInvoiceEdit>();

        ClientInDebts = new HashSet<ClientInDebt>();

        OrderItemBaseShiftStatuses = new HashSet<OrderItemBaseShiftStatus>();

        InputSaleMerges = new HashSet<SaleMerged>();

        OutputSaleMerges = new HashSet<SaleMerged>();

        IncomePaymentOrderSales = new HashSet<IncomePaymentOrderSale>();

        ShipmentListItems = new HashSet<ShipmentListItem>();

        ExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        UpdateDataCarrier = new HashSet<UpdateDataCarrier>();

        CountSaleMessages = new HashSet<CountSaleMessage>();

        RetailClientPaymentImages = new HashSet<RetailClientPaymentImage>();
    }

    public long OrderId { get; set; }

    public long? UserId { get; set; }

    public long? UpdateUserId { get; set; }

    public long ClientAgreementId { get; set; }

    public long BaseLifeCycleStatusId { get; set; }

    public long BaseSalePaymentStatusId { get; set; }

    public long? TaxFreePackListId { get; set; }

    public long? SadId { get; set; }

    public long? DeliveryRecipientAddressId { get; set; }

    public long? DeliveryRecipientId { get; set; }

    public long? TransporterId { get; set; }

    public long? SaleNumberId { get; set; }

    public long? ShiftStatusId { get; set; }

    public long? SaleInvoiceDocumentId { get; set; }

    public long? SaleInvoiceNumberId { get; set; }

    public long? ChangedToInvoiceById { get; set; }

    public long? RetailClientId { get; set; }

    public long? MisplacedSaleId { get; set; }
    public long? WarehousesShipmentId { get; set; }
    public long? WorkplaceId { get; set; }

    public long? CustomersOwnTtnId { get; set; }

    public string Comment { get; set; }

    public string OneTimeDiscountComment { get; set; }

    public bool IsMerged { get; set; }
    public bool IsInvoice { get; set; }
    public bool IsPrinted { get; set; }
    public bool IsPrintedActProtocolEdit { get; set; }

    public bool IsSent { get; set; }

    public bool IsPrintedPaymentInvoice { get; set; }

    public bool IsCashOnDelivery { get; set; }

    public bool HasDocuments { get; set; }

    public bool IsDevelopment { get; set; }

    public bool IsVatSale { get; set; }

    public bool IsLocked { get; set; }

    public bool IsPaymentBillDownloaded { get; set; }

    public DateTime? BillDownloadDate { get; set; }

    public bool IsFullPayment { get; set; }

    public bool IsImported { get; set; }

    public double ExpiredDays { get; set; }

    public string TTN { get; set; }

    public decimal ShippingAmount { get; set; }

    public decimal ShippingAmountEur { get; set; }

    public decimal CashOnDeliveryAmount { get; set; }

    public bool IsAcceptedToPacking { get; set; }

    //Ignored field
    public decimal TotalAmount { get; set; }

    public decimal TotalAmountEurToUah { get; set; }

    //Ignored field
    public decimal TotalAmountLocal { get; set; }

    //Ignored field
    public decimal VatAmount { get; set; }

    //Ignored field
    public decimal VatAmountPln { get; set; }

    //Ignored field
    public double TotalCount { get; set; }

    //Ignored field
    public double TotalWeight { get; set; }

    //Ignored field
    public string UserFullName { get; set; }

    //Ignored field
    public bool IsEdited { get; set; }

    //Ignored field
    public long TotalRowsQty { get; set; }

    public Guid? ParentNetId { get; set; }

    public DateTime? ChangedToInvoice { get; set; }

    public DateTime? ShipmentDate { get; set; }

    public WarehousesShipment WarehousesShipment { get; set; }

    public TaxFreePackList TaxFreePackList { get; set; }

    public SaleInvoiceDocument SaleInvoiceDocument { get; set; }

    public SaleBaseShiftStatus ShiftStatus { get; set; }

    public Order Order { get; set; }

    public User User { get; set; }

    public User UpdateUser { get; set; }

    public User ChangedToInvoiceBy { get; set; }

    public SaleNumber SaleNumber { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Transporter Transporter { get; set; }

    public DeliveryRecipient DeliveryRecipient { get; set; }

    public DeliveryRecipientAddress DeliveryRecipientAddress { get; set; }

    public BaseLifeCycleStatus BaseLifeCycleStatus { get; set; }

    public BaseSalePaymentStatus BaseSalePaymentStatus { get; set; }

    public SaleInvoiceNumber SaleInvoiceNumber { get; set; }

    public Sad Sad { get; set; }

    public RetailClient RetailClient { get; set; }

    public MisplacedSale MisplacedSale { get; set; }

    public Workplace Workplace { get; set; }

    public CustomersOwnTtn CustomersOwnTtn { get; set; }

    public ICollection<HistoryInvoiceEdit> HistoryInvoiceEdit { get; set; }

    public ICollection<SaleExchangeRate> SaleExchangeRates { get; set; }

    public ICollection<ClientInDebt> ClientInDebts { get; set; }

    public ICollection<OrderItemBaseShiftStatus> OrderItemBaseShiftStatuses { get; set; }

    public ICollection<SaleMerged> InputSaleMerges { get; set; }

    public ICollection<SaleMerged> OutputSaleMerges { get; set; }

    public ICollection<IncomePaymentOrderSale> IncomePaymentOrderSales { get; set; }

    public ICollection<ShipmentListItem> ShipmentListItems { get; set; }

    public ICollection<UpdateDataCarrier> UpdateDataCarrier { get; set; }

    public ICollection<ExpiredBillUserNotification> ExpiredBillUserNotifications { get; set; }

    public ICollection<CountSaleMessage> CountSaleMessages { get; set; }

    public ICollection<RetailClientPaymentImage> RetailClientPaymentImages { get; set; }
}