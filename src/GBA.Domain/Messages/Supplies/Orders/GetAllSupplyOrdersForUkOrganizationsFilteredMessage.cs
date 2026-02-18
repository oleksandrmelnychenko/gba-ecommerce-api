using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllSupplyOrdersForUkOrganizationsFilteredMessage {
    public GetAllSupplyOrdersForUkOrganizationsFilteredMessage(
        Guid? clientNetId,
        string value,
        DateTime from,
        DateTime to,
        string supplierName,
        long? currencyId,
        long limit,
        long offset) {
        ClientNetId = clientNetId;

        CurrencyId = currencyId;

        SupplierName = string.IsNullOrEmpty(supplierName) ? string.Empty : supplierName;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public Guid? ClientNetId { get; }

    public long? CurrencyId { get; }

    public string SupplierName { get; }

    public string Value { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }
}