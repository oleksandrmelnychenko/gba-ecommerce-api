using System;

namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class PaymentMovementInfoModel {
    public Guid NetId { get; set; }

    public bool IsIncrease { get; set; }

    public decimal Value { get; set; }

    public decimal InitialBalance { get; set; }

    public decimal FinalBalance { get; set; }

    public decimal ValueEur { get; set; }

    public decimal InitialBalanceEur { get; set; }

    public decimal FinalBalanceEur { get; set; }
}