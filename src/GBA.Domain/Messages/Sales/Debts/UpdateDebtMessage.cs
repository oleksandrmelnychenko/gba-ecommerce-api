using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.Debts;

public sealed class UpdateDebtMessage {
    public UpdateDebtMessage(Debt debt) {
        Debt = debt;
    }

    public Debt Debt { get; set; }
}