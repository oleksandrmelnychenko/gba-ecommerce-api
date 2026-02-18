using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.PaymentTasks;

public sealed class MergeSupplyPaymentTasksMessage {
    public MergeSupplyPaymentTasksMessage(IEnumerable<SupplyPaymentTask> tasks) {
        Tasks = tasks ?? new List<SupplyPaymentTask>();
    }

    public IEnumerable<SupplyPaymentTask> Tasks { get; }
}