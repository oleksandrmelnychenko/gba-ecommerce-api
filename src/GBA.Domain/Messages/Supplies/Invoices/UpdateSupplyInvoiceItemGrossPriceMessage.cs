using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class UpdateSupplyInvoiceItemGrossPriceMessage {
    public UpdateSupplyInvoiceItemGrossPriceMessage(
        IEnumerable<long> supplyInvoiceIds,
        Guid userNetId) {
        SupplyInvoiceIds = supplyInvoiceIds;
        UserNetId = userNetId;
    }

    public IEnumerable<long> SupplyInvoiceIds { get; }

    public Guid UserNetId { get; }
}