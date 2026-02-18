using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class Sad : EntityBase {
    public Sad() {
        SadDocuments = new HashSet<SadDocument>();

        SadItems = new HashSet<SadItem>();

        Sales = new HashSet<Sale>();

        SadPallets = new HashSet<SadPallet>();

        AdvancePayments = new HashSet<AdvancePayment>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        OrderProductSpecifications = new HashSet<OrderProductSpecification>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public decimal MarginAmount { get; set; }

    public double TotalQty { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public double SadCoefficient { get; set; }

    public decimal VatPercent { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public decimal TotalAmountWithMargin { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal TotalVatAmountWithMargin { get; set; }

    public SadType SadType { get; set; }

    public bool IsSend { get; set; }

    public bool IsFromSale { get; set; }

    public long? StathamId { get; set; }

    public long? StathamCarId { get; set; }

    public long? StathamPassportId { get; set; }

    public long? OrganizationId { get; set; }

    public long? SupplyOrderUkraineId { get; set; }

    public long? OrganizationClientId { get; set; }

    public long? OrganizationClientAgreementId { get; set; }

    public long? ClientId { get; set; }

    public long? ClientAgreementId { get; set; }

    public long ResponsibleId { get; set; }

    public Statham Statham { get; set; }

    public StathamCar StathamCar { get; set; }

    public StathamPassport StathamPassport { get; set; }

    public Organization Organization { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public OrganizationClient OrganizationClient { get; set; }

    public OrganizationClientAgreement OrganizationClientAgreement { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public User Responsible { get; set; }

    public ICollection<SadDocument> SadDocuments { get; set; }

    public ICollection<SadItem> SadItems { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<SadPallet> SadPallets { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<OrderProductSpecification> OrderProductSpecifications { get; set; }
}