namespace GBA.Domain.EntityHelpers.TotalDashboards;

public sealed class TotalItem {
    public decimal ValueByDay { get; set; }

    public decimal IncreaseByDay { get; set; }

    public bool IsIncreaseByDay { get; set; }

    public decimal ValueByMonth { get; set; }

    public decimal IncreaseByMonth { get; set; }

    public bool IsIncreaseByMonth { get; set; }
}