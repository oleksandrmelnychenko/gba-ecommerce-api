using System;

namespace GBA.Domain.Messages.Debtors;

public sealed class GetAllDebtorsFilteredMessage {
    public GetAllDebtorsFilteredMessage(string value, bool allDebtors, Guid userNetId, long limit, long offset) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;

        AllDebtors = allDebtors;

        UserNetId = userNetId;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string Value { get; }

    public bool AllDebtors { get; }

    public Guid UserNetId { get; }

    public long Limit { get; }

    public long Offset { get; }
}