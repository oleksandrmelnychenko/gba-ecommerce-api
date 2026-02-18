using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.Ukraine.AppliedActions;

public sealed class ActReconciliationAppliedAction {
    public ActReconciliationAppliedAction() {
        Items = new List<ActReconciliationAppliedActionItem>();
    }

    public ActReconciliationItem ActReconciliationItem { get; set; }

    public List<ActReconciliationAppliedActionItem> Items { get; set; }
}