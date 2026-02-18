using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class UpdateDeliveryExpensesMessage {
    public UpdateDeliveryExpensesMessage(DeliveryExpense deliveryExpense, Guid userNetId) {
        DeliveryExpense = deliveryExpense;
        UserNetId = userNetId;
    }

    public DeliveryExpense DeliveryExpense { get; }
    public Guid UserNetId { get; }
}