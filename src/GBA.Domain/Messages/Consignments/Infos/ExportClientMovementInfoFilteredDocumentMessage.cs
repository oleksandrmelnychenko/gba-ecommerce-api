using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class ExportClientMovementInfoFilteredDocumentMessage {
    public ExportClientMovementInfoFilteredDocumentMessage(
        string pathToFolder,
        Guid clientNetId,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;

        ClientNetId = clientNetId;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = (to.Year.Equals(1) ? DateTime.UtcNow.Date : to.Date).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }

    public Guid ClientNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}