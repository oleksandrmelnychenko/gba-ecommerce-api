using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.EntityHelpers.Consignments;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class ExportMovementInfoDocumentMessage {
    public ExportMovementInfoDocumentMessage(
        string pathToFolder,
        IEnumerable<ConsignmentItemMovementType> movementTypes,
        Guid productNetId,
        DateTime from,
        DateTime to,
        ConsignmentMovementType movementType) {
        PathToFolder = pathToFolder;

        MovementTypes = movementTypes;

        ProductNetId = productNetId;
        MovementType = movementType;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = (to.Year.Equals(1) ? DateTime.UtcNow.Date : to.Date).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }

    public IEnumerable<ConsignmentItemMovementType> MovementTypes { get; }

    public Guid ProductNetId { get; }

    public ConsignmentMovementType MovementType { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}