using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.ActReconciliations;

public sealed class GetAllActReconciliationsFilteredMessage {
    public GetAllActReconciliationsFilteredMessage(DateTime from, DateTime to) {
        From = from.Year.Equals(1) ? DateTime.Now.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime From { get; }

    public DateTime To { get; }
}