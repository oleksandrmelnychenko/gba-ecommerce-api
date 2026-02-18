using System;

namespace GBA.Domain.Messages.TotalDashboards;

public sealed class GetStateCurrencyRegisterByPeriodMessage {
    public GetStateCurrencyRegisterByPeriodMessage(
        DateTime? from,
        DateTime? to) {
        From = from?.Date ?? DateTime.Now;
        To = to?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ??
             DateTime.Now.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime From { get; }
    public DateTime To { get; }
}