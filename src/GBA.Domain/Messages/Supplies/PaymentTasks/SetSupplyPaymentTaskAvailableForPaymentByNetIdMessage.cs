using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.PaymentTasks;

public sealed class SetSupplyPaymentTaskAvailableForPaymentByNetIdMessage {
    public SetSupplyPaymentTaskAvailableForPaymentByNetIdMessage(SupplyPaymentTask supplyPaymentTask) {
        SupplyPaymentTask = supplyPaymentTask;
    }

    public SupplyPaymentTask SupplyPaymentTask { get; }
}