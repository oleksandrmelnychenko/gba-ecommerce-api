namespace GBA.Domain.EntityHelpers;

public sealed class TotalBySalesManagers {
    public string LastName { get; set; } = string.Empty;

    public int TotalSalesCount { get; set; } = 0;

    public decimal TotalSalesAmount { get; set; } = decimal.Zero;
}