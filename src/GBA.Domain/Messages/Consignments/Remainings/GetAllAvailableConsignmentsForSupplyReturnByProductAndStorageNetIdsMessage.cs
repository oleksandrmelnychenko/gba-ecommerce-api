using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage {
    public GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage(Guid productNetId, Guid storageNetId) {
        ProductNetId = productNetId;

        StorageNetId = storageNetId;
    }

    public Guid ProductNetId { get; }

    public Guid StorageNetId { get; }
}