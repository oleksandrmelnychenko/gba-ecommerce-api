namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class TotalValueByPeriod {
    public decimal InitialBalance { get; set; }

    public decimal Receipts { get; set; }

    public decimal Expense { get; set; }

    public decimal FinalBalance { get; set; }
}