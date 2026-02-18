using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllTotalAmountMessage {
    public GetAllTotalAmountMessage(
        SaleLifeCycleType? saleLifeCycleType,
        DateTime? from,
        DateTime? to) {
        SaleLifeCycleType = saleLifeCycleType;
        From = from;
        To = to;
    }

    public SaleLifeCycleType? SaleLifeCycleType { get; set; }

    public QueryType Type { get; set; }

    public Guid? UserNetId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}