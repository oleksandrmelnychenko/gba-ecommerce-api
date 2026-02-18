using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetLastUsedProductPlacementByProductAndStorageNetIdsMessage {
    public GetLastUsedProductPlacementByProductAndStorageNetIdsMessage(
        Guid storageNetId,
        Guid productNetId
    ) {
        StorageNetId = storageNetId;

        ProductNetId = productNetId;
    }

    public Guid StorageNetId { get; }

    public Guid ProductNetId { get; }
}