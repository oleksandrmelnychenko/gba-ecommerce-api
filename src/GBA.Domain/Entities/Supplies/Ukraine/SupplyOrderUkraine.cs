using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SupplyOrderUkraine : EntityBase {
    public SupplyOrderUkraine() {
        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();

        ActReconciliations = new HashSet<ActReconciliation>();

        DynamicProductPlacementColumns = new HashSet<DynamicProductPlacementColumn>();

        MergedServices = new HashSet<MergedService>();

        SupplyOrderUkrainePaymentDeliveryProtocols = new HashSet<SupplyOrderUkrainePaymentDeliveryProtocol>();

        SupplyOrderUkraineDocuments = new HashSet<SupplyOrderUkraineDocument>();

        DeliveryExpenses = new HashSet<DeliveryExpense>();
    }

    public DateTime FromDate { get; set; }

    public DateTime InvDate { get; set; }

    public DateTime? AdditionalPaymentFromDate { get; set; }

    public bool IsPlaced { get; set; }

    public bool IsPartialPlaced { get; set; }

    public bool IsDirectFromSupplier { get; set; }

    public string Number { get; set; }

    public string InvNumber { get; set; }

    public string Comment { get; set; }

    public decimal ShipmentAmount { get; set; }

    public decimal ShipmentAmountLocal { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal TotalNetPriceLocal { get; set; }

    public decimal TotalNetPriceLocalWithVat { get; set; }

    public decimal TotalGrossPriceLocal { get; set; }

    public decimal AdditionalAmount { get; set; }

    public double AdditionalPercent { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }
    public decimal TotalDeliveryExpenseAmount { get; set; }

    public decimal TotalAccountingDeliveryExpenseAmount { get; set; }

    public double TotalQty { get; set; }

    public long ResponsibleId { get; set; }

    public long OrganizationId { get; set; }

    public long SupplierId { get; set; }

    public long ClientAgreementId { get; set; }

    public long? AdditionalPaymentCurrencyId { get; set; }

    public decimal VatPercent { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal TotalAccountingGrossPrice { get; set; }

    public decimal TotalAccountingGrossPriceLocal { get; set; }

    public int TotalRowsQty { get; set; }

    public User Responsible { get; set; }

    public Organization Organization { get; set; }

    public Client Supplier { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public TaxFreePackList TaxFreePackList { get; set; }

    public Sad Sad { get; set; }

    public Currency AdditionalPaymentCurrency { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    public ICollection<ActReconciliation> ActReconciliations { get; set; }

    public ICollection<DynamicProductPlacementColumn> DynamicProductPlacementColumns { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<SupplyOrderUkrainePaymentDeliveryProtocol> SupplyOrderUkrainePaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyOrderUkraineDocument> SupplyOrderUkraineDocuments { get; set; }

    public ICollection<DeliveryExpense> DeliveryExpenses { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public decimal TotalProtocolsValue { get; set; }

    public double TotalProtocolsDiscount { get; set; }
}