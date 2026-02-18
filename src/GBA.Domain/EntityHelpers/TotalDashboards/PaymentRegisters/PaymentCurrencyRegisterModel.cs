using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class PaymentCurrencyRegisterModel {
    public Guid NetUId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? FromDate { get; set; }

    public Organization Organization { get; set; }

    public Currency Currency { get; set; }

    public PaymentRegister PaymentRegister { get; set; }

    public List<PaymentMovement> PaymentMovements { get; set; }

    public TotalValueByPeriod TotalValue { get; set; }

    public TotalValueByPeriod TotalValueEur { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}