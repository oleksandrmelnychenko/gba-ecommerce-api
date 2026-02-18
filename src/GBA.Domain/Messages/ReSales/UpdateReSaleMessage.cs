using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class UpdateReSaleMessage {
    public UpdateReSaleMessage(
        UpdatedReSaleModel updatedReSale) {
        UpdatedReSale = updatedReSale;
    }

    public UpdatedReSaleModel UpdatedReSale { get; }
}