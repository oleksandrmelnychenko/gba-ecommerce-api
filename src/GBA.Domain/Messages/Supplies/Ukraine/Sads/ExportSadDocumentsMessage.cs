using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class ExportSadDocumentsMessage {
    public ExportSadDocumentsMessage(string path, Guid netId, Guid userNetId) {
        Path = path;

        NetId = netId;

        UserNetId = userNetId;
    }

    public string Path { get; }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}