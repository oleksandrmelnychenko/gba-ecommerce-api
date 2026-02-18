using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSalesRegisterByClientNetIdMessage {
    public GetSalesRegisterByClientNetIdMessage(
        Guid clientNetId,
        SaleRegisterType? saleRegisterType,
        string value,
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        Guid userNetId) {
        ClientNetId = clientNetId;
        SaleRegisterType = saleRegisterType;
        Value = value;
        From = from;
        To = to;
        Limit = limit;
        Offset = offset;
        UserNetId = userNetId;
    }

    public Guid ClientNetId { get; }

    public SaleRegisterType? SaleRegisterType { get; }

    public string Value { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public int Limit { get; }

    public int Offset { get; }

    public Guid UserNetId { get; }
}