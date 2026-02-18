using System;

namespace GBA.Domain.Messages.Auditing;

public sealed class GetAllAuditDataByNetIdLimitedMessage {
    public GetAllAuditDataByNetIdLimitedMessage(Guid netId, long limit, long offset, string fieldName = "") {
        NetId = netId;

        Limit = limit > 0 ? limit : 20;

        Offset = offset < 0 ? 0 : offset;

        FieldName = fieldName;
    }

    public Guid NetId { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string FieldName { get; }
}