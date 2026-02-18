using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Containers;

public sealed class GetAllContainersRangedMessage {
    public GetAllContainersRangedMessage(DateTime? from, DateTime? to) {
        From = from;
        To = to;
    }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}