using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesByClientNetIdMessage {
    public GetAllSalesByClientNetIdMessage(Guid clientNetId, SaleLifeCycleType? saleLifeCycleType, string value, DateTime? from, DateTime? to, Guid userNetId) {
        ClientNetId = clientNetId;

        SaleLifeCycleType = saleLifeCycleType;

        Value = value;

        From = from;

        To = to;

        UserNetId = userNetId;
    }

    public Guid ClientNetId { get; set; }

    public SaleLifeCycleType? SaleLifeCycleType { get; set; }

    public string Value { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public Guid UserNetId { get; set; }
}