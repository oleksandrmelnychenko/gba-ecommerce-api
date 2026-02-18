using System;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class GetUpdatedReSaleByNetIdMessage {
    public GetUpdatedReSaleByNetIdMessage(
        UpdatedReSaleModel updatedReSaleModel,
        Guid netId) {
        UpdatedReSaleModel = updatedReSaleModel;
        NetId = netId;
    }

    public UpdatedReSaleModel UpdatedReSaleModel { get; }
    public Guid NetId { get; }
}