using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class GetAllTaxFreesFilteredMessage {
    public GetAllTaxFreesFilteredMessage(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        string value,
        TaxFreeStatus? status,
        Guid? stathamNetId
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;

        Status = status;

        StathamNetId = stathamNetId;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string Value { get; }

    public TaxFreeStatus? Status { get; }

    public Guid? StathamNetId { get; }
}