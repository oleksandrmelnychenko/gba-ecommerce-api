using System;
using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class PaymentMovement {
    public Guid NetUId { get; set; }

    public DateTime Created { get; set; }

    public DateTime FromDate { get; set; }

    public string Number { get; set; }

    public TypePaymentMovement Type { get; set; }

    public decimal Value { get; set; }

    public decimal ValueEur { get; set; }

    public bool IsIncrease { get; set; }

    public string Comment { get; set; }

    public Currency Currency { get; set; }

    public User User { get; set; }

    public decimal InitialBalance { get; set; }

    public decimal FinalBalance { get; set; }

    public decimal InitialBalanceEur { get; set; }

    public decimal FinalBalanceEur { get; set; }

    public string ToPaymentRegisterName { get; set; }

    public string FromPaymentRegisterName { get; set; }

    public string ClientName { get; set; }

    public string IsAccounting { get; set; }
}