using System;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllFromSearchMessage {
    public GetAllFromSearchMessage(OrderFilterType type, Guid? clientNetId, string value, string documentType, DateTime from, DateTime to, long limit, long offset) {
        Type = type;

        ClientNetId = clientNetId;

        Value = value;

        DocumentType = documentType;

        From = from;

        To = to;

        Limit = limit;

        Offset = offset;
    }

    public OrderFilterType Type { get; set; }

    public Guid? ClientNetId { get; set; }

    public string Value { get; set; }

    public string DocumentType { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}