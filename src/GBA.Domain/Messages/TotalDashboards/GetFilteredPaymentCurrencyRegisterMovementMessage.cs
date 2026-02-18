using System;
using GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

namespace GBA.Domain.Messages.TotalDashboards;

public sealed class GetFilteredPaymentCurrencyRegisterMovementMessage {
    public GetFilteredPaymentCurrencyRegisterMovementMessage(
        Guid netId,
        TypeFilteredMovements typeFilteredMovements,
        DateTime? from,
        DateTime? to,
        int limit,
        int offset) {
        NetId = netId;
        TypeFilteredMovements = typeFilteredMovements;
        Limit = limit;
        Offset = offset;
        From = from?.Date ?? DateTime.Now;
        To = to?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public Guid NetId { get; }
    public TypeFilteredMovements TypeFilteredMovements { get; }
    public int Limit { get; }
    public int Offset { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}