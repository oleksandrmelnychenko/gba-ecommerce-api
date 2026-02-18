using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class GetAllSupplyOrdersUkraineFilteredMessage {
    public GetAllSupplyOrdersUkraineFilteredMessage(
        DateTime from,
        DateTime to,
        string supplierName,
        long? currencyId,
        long limit,
        long offset,
        bool nonPlaced
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;

        SupplierName = string.IsNullOrEmpty(supplierName) ? string.Empty : supplierName;

        CurrencyId = currencyId;

        NonPlaced = nonPlaced;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string SupplierName { get; }
    public long? CurrencyId { get; }
    public bool NonPlaced { get; }
}