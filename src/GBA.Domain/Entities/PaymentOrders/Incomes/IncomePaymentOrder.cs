using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class IncomePaymentOrder : EntityBase {
    public IncomePaymentOrder() {
        IncomePaymentOrderSales = new HashSet<IncomePaymentOrderSale>();

        AssignedPaymentOrders = new HashSet<AssignedPaymentOrder>();
    }

    public string Number { get; set; }

    public string ArrivalNumber { get; set; }

    public string BankAccount { get; set; }

    public string Comment { get; set; }

    public string PaymentPurpose { get; set; }

    public DateTime FromDate { get; set; }

    public IncomePaymentOrderType IncomePaymentOrderType { get; set; }

    public double VatPercent { get; set; }

    public decimal VAT { get; set; }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal AgreementExchangedAmount { get; set; }

    public decimal AgreementEuroExchangeRate { get; set; }

    public decimal EuroAmount { get; set; }

    public decimal OverpaidAmount { get; set; }

    public bool IsManagementAccounting { get; set; }

    public bool IsAccounting { get; set; }

    public bool IsUpdated { get; set; }

    public bool IsCanceled { get; set; }

    public int Account { get; set; }

    public long OrganizationId { get; set; }

    public long CurrencyId { get; set; }

    public long PaymentRegisterId { get; set; }

    public long UserId { get; set; }

    public long? ClientId { get; set; }

    public OperationType OperationType { get; set; }

    // Ignored
    public string OperationTypeName { get; set; }

    public long? ClientAgreementId { get; set; }

    public long? SupplyOrganizationId { get; set; }

    public long? SupplyOrganizationAgreementId { get; set; }

    public long? ColleagueId { get; set; }

    public long? OrganizationClientId { get; set; }

    public long? OrganizationClientAgreementId { get; set; }

    public long? TaxFreeId { get; set; }

    public long? SadId { get; set; }

    public Organization Organization { get; set; }

    public Currency Currency { get; set; }

    public PaymentRegister PaymentRegister { get; set; }

    public PaymentMovementOperation PaymentMovementOperation { get; set; }

    public User User { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public User Colleague { get; set; }

    public OrganizationClient OrganizationClient { get; set; }

    public OrganizationClientAgreement OrganizationClientAgreement { get; set; }

    public TaxFree TaxFree { get; set; }

    public Sad Sad { get; set; }

    public AssignedPaymentOrder RootAssignedPaymentOrder { get; set; }

    public ICollection<AssignedPaymentOrder> AssignedPaymentOrders { get; set; }

    public ICollection<IncomePaymentOrderSale> IncomePaymentOrderSales { get; set; }

    public int TotalQty { get; set; }
}