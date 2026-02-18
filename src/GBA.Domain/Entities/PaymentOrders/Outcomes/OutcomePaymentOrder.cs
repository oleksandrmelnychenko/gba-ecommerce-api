using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class OutcomePaymentOrder : EntityBase {
    public OutcomePaymentOrder() {
        OutcomePaymentOrderConsumablesOrders = new HashSet<OutcomePaymentOrderConsumablesOrder>();

        AssignedPaymentOrders = new HashSet<AssignedPaymentOrder>();

        CompanyCarFuelings = new HashSet<CompanyCarFueling>();

        CompanyCarRoadLists = new HashSet<CompanyCarRoadList>();

        OutcomePaymentOrderSupplyPaymentTasks = new HashSet<OutcomePaymentOrderSupplyPaymentTask>();
    }

    public string Number { get; set; }

    public string CustomNumber { get; set; }

    public string AdvanceNumber { get; set; }

    public string ArrivalNumber { get; set; }

    public string PaymentPurpose { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public decimal Amount { get; set; }

    public decimal AfterExchangeAmount { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal EuroAmount { get; set; }

    public decimal DifferenceAmount { get; set; }

    public decimal VAT { get; set; }

    public double AddedFuelAmount { get; set; }

    public double SpentFuelAmount { get; set; }

    public double VatPercent { get; set; }

    public int Account { get; set; }

    public long UserId { get; set; }

    public long OrganizationId { get; set; }

    public long PaymentCurrencyRegisterId { get; set; }

    public long? ColleagueId { get; set; }

    public long? ConsumableProductOrganizationId { get; set; }

    public long? ClientAgreementId { get; set; }

    public long? ClientId { get; set; }

    public long? SupplyOrderPolandPaymentDeliveryProtocolId { get; set; }

    public long? SupplyOrganizationAgreementId { get; set; }

    public long? OrganizationClientId { get; set; }

    public long? OrganizationClientAgreementId { get; set; }

    public long? TaxFreeId { get; set; }

    public long? SadId { get; set; }

    public bool IsUpdated { get; set; }

    public bool IsUnderReport { get; set; }

    public bool IsUnderReportDone { get; set; }

    public bool IsCanceled { get; set; }

    public bool IsManagementAccounting { get; set; }

    public bool IsAccounting { get; set; }

    public OperationType OperationType { get; set; }

    // Ignored
    public string OperationTypeName { get; set; }

    public int TotalRowsQty { get; set; }

    public User User { get; set; }

    public Organization Organization { get; set; }

    public PaymentCurrencyRegister PaymentCurrencyRegister { get; set; }

    public PaymentMovementOperation PaymentMovementOperation { get; set; }

    public User Colleague { get; set; }

    public SupplyOrganization ConsumableProductOrganization { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public SupplyOrderPolandPaymentDeliveryProtocol SupplyOrderPolandPaymentDeliveryProtocol { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public OrganizationClient OrganizationClient { get; set; }

    public OrganizationClientAgreement OrganizationClientAgreement { get; set; }

    public TaxFree TaxFree { get; set; }

    public Sad Sad { get; set; }

    public AssignedPaymentOrder RootAssignedPaymentOrder { get; set; }

    public ICollection<AssignedPaymentOrder> AssignedPaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrderConsumablesOrder> OutcomePaymentOrderConsumablesOrders { get; set; }

    public ICollection<CompanyCarFueling> CompanyCarFuelings { get; set; }

    public ICollection<CompanyCarRoadList> CompanyCarRoadLists { get; set; }

    public ICollection<OutcomePaymentOrderSupplyPaymentTask> OutcomePaymentOrderSupplyPaymentTasks { get; set; }
}