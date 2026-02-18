using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class PaymentRegister : EntityBase {
    public PaymentRegister() {
        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        PaymentCurrencyRegisters = new HashSet<PaymentCurrencyRegister>();
    }

    public string Name { get; set; }

    public PaymentRegisterType Type { get; set; }

    public string AccountNumber { get; set; }

    public string SortCode { get; set; }

    public string IBAN { get; set; }

    public string SwiftCode { get; set; }

    public string BankName { get; set; }

    public string CVV { get; set; }

    public string City { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public decimal TotalEuroAmount { get; set; }

    public bool IsActive { get; set; }

    public long OrganizationId { get; set; }

    public bool IsMain { get; set; }

    public bool IsForRetail { get; set; }

    public bool IsSelected { get; set; }

    public Organization Organization { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<PaymentCurrencyRegister> PaymentCurrencyRegisters { get; set; }
}