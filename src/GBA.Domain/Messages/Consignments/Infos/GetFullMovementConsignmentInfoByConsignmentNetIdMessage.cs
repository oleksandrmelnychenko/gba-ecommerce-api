using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class GetFullMovementConsignmentInfoByConsignmentNetIdMessage {
    public GetFullMovementConsignmentInfoByConsignmentNetIdMessage(
        Guid consignmentItemNetId,
        DateTime from,
        DateTime to) {
        ConsignmentItemNetId = consignmentItemNetId;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = (to.Year.Equals(1) ? DateTime.UtcNow.Date : to.Date).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public Guid ConsignmentItemNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}