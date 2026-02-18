using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetFilteredSubGroupsProductGroupMessage {
    public GetFilteredSubGroupsProductGroupMessage(
        Guid netId,
        int limit,
        int offset,
        string value) {
        NetId = netId;
        Limit = limit;
        Offset = offset;
        Value = value ?? string.Empty;
    }

    public Guid NetId { get; }
    public int Limit { get; }
    public int Offset { get; }
    public string Value { get; }
}