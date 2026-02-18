namespace GBA.Domain.Messages.Consignments;

public sealed class AddNewConsignmentFromProductTransferMessage {
    public AddNewConsignmentFromProductTransferMessage(long productTransferId) {
        ProductTransferId = productTransferId;
    }

    public long ProductTransferId { get; }
}