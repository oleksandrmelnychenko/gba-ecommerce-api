using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Messages.Storages;

public sealed class GetAllStoragesForReturnsMessage {
    public GetAllStoragesForReturnsMessage(SaleReturnItemStatus status) {
        Status = status;
    }

    public SaleReturnItemStatus Status { get; }
}