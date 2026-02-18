using System;

namespace GBA.Domain.Messages.Storages;

public sealed class ExportAllProductsByStorageDocumentMessage {
    public ExportAllProductsByStorageDocumentMessage(
        string pathToFoler,
        Guid netId) {
        PathToFolder = pathToFoler;
        NetId = netId;
    }

    public string PathToFolder { get; }

    public Guid NetId { get; }
}