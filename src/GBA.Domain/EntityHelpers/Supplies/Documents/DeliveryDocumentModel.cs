using System;

namespace GBA.Domain.EntityHelpers.Supplies.Documents;

public sealed class DeliveryDocumentModel {
    public string Name { get; set; }

    public DateTime FromDate { get; set; }

    public string DocumentNumber { get; set; }
}