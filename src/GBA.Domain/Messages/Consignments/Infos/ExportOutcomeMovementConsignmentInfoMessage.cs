using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class ExportOutcomeMovementConsignmentInfoMessage {
    public ExportOutcomeMovementConsignmentInfoMessage(
        string pathToFolder,
        Guid productNetId,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;

        ProductNetId = productNetId;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = (to.Year.Equals(1) ? DateTime.UtcNow.Date : to.Date).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }

    public Guid ProductNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}