using System;

namespace GBA.Domain.Messages.GbaData;

public sealed class GetAllProductCapitalizationsForExportMessage {
    public GetAllProductCapitalizationsForExportMessage(DateTime from, DateTime to) {
        From = from.Year.Equals(1) ? DateTime.Now.Date : from;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime From { get; }

    public DateTime To { get; }
}