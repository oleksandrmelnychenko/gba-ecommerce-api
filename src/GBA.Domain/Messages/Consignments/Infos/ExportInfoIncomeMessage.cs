using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class ExportInfoIncomeMessage {
    public ExportInfoIncomeMessage(
        Guid productNetId) {
        ProductNetId = productNetId;
    }

    public Guid ProductNetId { get; set; }
}