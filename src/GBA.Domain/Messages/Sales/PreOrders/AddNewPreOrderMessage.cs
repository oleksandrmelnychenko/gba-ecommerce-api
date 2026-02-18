using System;

namespace GBA.Domain.Messages.Sales.PreOrders;

public sealed class AddNewPreOrderMessage {
    public AddNewPreOrderMessage(Guid productNetId, Guid clientAgreementNetId, double qty, string comment) {
        ProductNetId = productNetId;

        ClientAgreementNetId = clientAgreementNetId;

        Qty = qty;

        Comment = comment;
    }

    public Guid ProductNetId { get; }

    public Guid ClientAgreementNetId { get; }

    public double Qty { get; }

    public string Comment { get; }
}