using GBA.Domain.Entities.Clients;

namespace GBA.Domain.EntityHelpers;

public sealed class Debtor {
    public Client Client { get; set; }

    public decimal TotalDebtForToday { get; set; }

    public decimal TotalDebtForMonthEnd { get; set; }

    public decimal Solvency { get; set; }
}