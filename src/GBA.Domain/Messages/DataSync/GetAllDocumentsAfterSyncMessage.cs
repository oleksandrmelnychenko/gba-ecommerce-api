using System;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Messages.DataSync;

public sealed class GetAllDocumentsAfterSyncMessage {
    public GetAllDocumentsAfterSyncMessage(
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        string name,
        ContractorType type) {
        From = from;
        To = to;
        Limit = limit;
        Offset = offset;
        Name = string.IsNullOrEmpty(name) ? "" : name;
        Type = type;
    }

    public DateTime From { get; }
    public DateTime To { get; }
    public int Limit { get; }
    public int Offset { get; }
    public string Name { get; }
    public ContractorType Type { get; }
}