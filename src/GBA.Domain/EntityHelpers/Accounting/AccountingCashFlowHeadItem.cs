using System;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class AccountingCashFlowHeadItem {
    public SupplyOrderPaymentDeliveryProtocol SupplyOrderPaymentDeliveryProtocol { get; set; }

    public SupplyOrderPolandPaymentDeliveryProtocol SupplyOrderPolandPaymentDeliveryProtocol { get; set; }

    public ContainerService ContainerService { get; set; }

    public VehicleService VehicleService { get; set; }

    public CustomService CustomService { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public ConsumablesOrder ConsumablesOrder { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }

    public SaleReturn SaleReturn { get; set; }

    public Sale Sale { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }

    public MergedService MergedService { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }

    public SupplyOrderUkrainePaymentDeliveryProtocol SupplyOrderUkrainePaymentDeliveryProtocol { get; set; }

    public ProductIncome ProductIncome { get; set; }

    public UpdatedReSaleModel UpdatedReSaleModel { get; set; }

    public DeliveryExpense DeliveryExpense { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal CurrentBalanceEuro { get; set; }

    public decimal CurrentValue { get; set; }

    public bool IsCreditValue { get; set; }

    public bool IsAccounting { get; set; }

    public bool IsManagementAccounting { get; set; }

    public JoinServiceType Type { get; set; }

    public long Id { get; set; }

    public DateTime FromDate { get; set; }

    public string Number { get; set; }

    public string Name { get; set; }

    public string OrganizationName { get; set; }

    public string ResponsibleName { get; set; }

    public string Comment { get; set; }
}