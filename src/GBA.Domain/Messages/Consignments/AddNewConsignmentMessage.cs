namespace GBA.Domain.Messages.Consignments;

public sealed class AddNewConsignmentMessage {
    public AddNewConsignmentMessage(long productIncomeId, bool isVirtual) {
        ProductIncomeId = productIncomeId;

        IsVirtual = isVirtual;
    }

    public long ProductIncomeId { get; }

    public bool IsVirtual { get; }
}