using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.Debts;

public sealed class AddDebtMessage {
    public AddDebtMessage(Debt debt) {
        Debt = debt;
    }

    public Debt Debt { get; set; }
}