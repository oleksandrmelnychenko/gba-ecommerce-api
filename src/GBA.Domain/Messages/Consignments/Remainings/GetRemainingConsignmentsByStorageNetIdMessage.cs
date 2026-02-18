using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetRemainingConsignmentsByStorageNetIdMessage {
    public GetRemainingConsignmentsByStorageNetIdMessage(Guid storageNetId) {
        StorageNetId = storageNetId;
    }

    public Guid StorageNetId { get; }
}