using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetRegisterInvoiceMessage {
    public GetRegisterInvoiceMessage(
        DateTime from,
        DateTime to,
        string value,
        int limit,
        int offset) {
        From = from;
        To = to;
        Value = value;
        Limit = limit;
        Offset = offset;
    }

    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Value { get; set; }
    public int Limit { get; }
    public int Offset { get; }
}