using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Entities.Supplies.Ukraine.AppliedActions;

public sealed class ActReconciliationAppliedActionItem {
    public ActReconciliationAppliedActionType ActionType { get; set; }

    public ProductIncome ProductIncome { get; set; }

    public ProductTransfer ProductTransfer { get; set; }

    public DepreciatedOrder DepreciatedOrder { get; set; }
}