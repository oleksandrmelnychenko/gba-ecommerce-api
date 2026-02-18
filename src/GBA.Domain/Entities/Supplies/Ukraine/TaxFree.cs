using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class TaxFree : EntityBase {
    public TaxFree() {
        TaxFreeDocuments = new HashSet<TaxFreeDocument>();

        TaxFreeItems = new HashSet<TaxFreeItem>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        AdvancePayments = new HashSet<AdvancePayment>();
    }

    public string Number { get; set; }

    public string CustomCode { get; set; }

    public string Comment { get; set; }

    public decimal AmountPayedStatham { get; set; }

    public decimal AmountInPLN { get; set; }

    public decimal VatAmountInPLN { get; set; }

    public decimal AmountInEur { get; set; }

    public decimal MarginAmount { get; set; }

    public decimal VatPercent { get; set; }

    public double Weight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal UnitPriceWithVat { get; set; }

    public decimal TotalWithVat { get; set; }

    public decimal VatAmountPl { get; set; }

    public decimal TotalWithVatPl { get; set; }

    public TaxFreeStatus TaxFreeStatus { get; set; }

    public DateTime? DateOfPrint { get; set; }

    public DateTime? DateOfIssue { get; set; }

    public DateTime? DateOfStathamPayment { get; set; }

    public DateTime? DateOfTabulation { get; set; }

    public DateTime? FormedDate { get; set; }

    public DateTime? SelectedDate { get; set; }

    public DateTime? ReturnedDate { get; set; }

    public DateTime? ClosedDate { get; set; }

    public DateTime? CanceledDate { get; set; }

    public long? StathamId { get; set; }

    public long? StathamCarId { get; set; }

    public long? StathamPassportId { get; set; }

    public long TaxFreePackListId { get; set; }

    public long ResponsibleId { get; set; }

    public Statham Statham { get; set; }

    public StathamCar StathamCar { get; set; }

    public StathamPassport StathamPassport { get; set; }

    public TaxFreePackList TaxFreePackList { get; set; }

    public User Responsible { get; set; }

    public ICollection<TaxFreeDocument> TaxFreeDocuments { get; set; }

    public ICollection<TaxFreeItem> TaxFreeItems { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }
}