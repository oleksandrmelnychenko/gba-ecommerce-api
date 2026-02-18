using System;

namespace GBA.Domain.Messages.Consumables.Orders.Depreciated;

public sealed class GetAllDepreciatedConsumableOrdersFilteredMessage {
    public GetAllDepreciatedConsumableOrdersFilteredMessage(DateTime from, DateTime to, string value, Guid? storageNetId) {
        From = from;

        To = to;

        Value = value;

        StorageNetId = storageNetId;
    }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Value { get; set; }

    public Guid? StorageNetId { get; set; }
}