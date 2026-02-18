using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;

namespace GBA.Domain.Entities.ReSales;

public sealed class ReSale : EntityBase {
    public ReSale() {
        ReSaleItems = new HashSet<ReSaleItem>();

        ClientInDebts = new HashSet<ClientInDebt>();

        IncomePaymentOrderSales = new HashSet<IncomePaymentOrderSale>();
    }

    public string Comment { get; set; }

    public long? ClientAgreementId { get; set; }

    public long? ChangedToInvoiceById { get; set; }

    public long OrganizationId { get; set; }

    public long UserId { get; set; }

    public long? SaleNumberId { get; set; }

    public long BaseLifeCycleStatusId { get; set; }

    public long BaseSalePaymentStatusId { get; set; }

    public long FromStorageId { get; set; }

    public bool IsCompleted { get; set; }

    public decimal TotalPaymentAmount { get; set; }

    public BaseLifeCycleStatus BaseLifeCycleStatus { get; set; }

    public BaseSalePaymentStatus BaseSalePaymentStatus { get; set; }

    public DateTime? ChangedToInvoice { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }

    public User User { get; set; }

    public SaleNumber SaleNumber { get; set; }

    public ICollection<ReSaleItem> ReSaleItems { get; set; }

    public ICollection<ClientInDebt> ClientInDebts { get; set; }

    public ICollection<IncomePaymentOrderSale> IncomePaymentOrderSales { get; set; }

    public Storage FromStorage { get; set; }

    public User ChangedToInvoiceBy { get; set; }

    public decimal TotalAmount { get; set; }

    public double TotalQty { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public decimal TotalAmountEurToUah { get; set; }

    public decimal TotalVat { get; set; }

    public string UserFullName { get; set; }

    public decimal DifferencePaymentAndInvoiceAmount { get; set; }
}