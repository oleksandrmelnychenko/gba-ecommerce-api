using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public record MergeInvoicesMessage {
    public Guid NetId { get; init; }

    public IEnumerable<Guid> InvoiceNetIds { get; set; }
}