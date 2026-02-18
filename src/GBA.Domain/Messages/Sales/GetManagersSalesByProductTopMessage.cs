using System;
using GBA.Domain.EntityHelpers.SalesModels;

namespace GBA.Domain.Messages.Sales;

public sealed class GetManagersSalesByProductTopMessage {
    public GetManagersSalesByProductTopMessage(
        DateTime from,
        DateTime to,
        TypeOfProductTop typeProductTop) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        TypeProductTop = typeProductTop;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public TypeOfProductTop TypeProductTop { get; }
}