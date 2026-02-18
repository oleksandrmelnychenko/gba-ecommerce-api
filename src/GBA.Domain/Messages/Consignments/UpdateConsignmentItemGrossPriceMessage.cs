using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Consignments;

public sealed class UpdateConsignmentItemGrossPriceMessage {
    public UpdateConsignmentItemGrossPriceMessage(
        IEnumerable<long> supplyInvoiceIds,
        Guid userNetId) {
        SupplyInvoiceIds = supplyInvoiceIds;
        UserNetId = userNetId;
    }

    public IEnumerable<long> SupplyInvoiceIds { get; }

    public Guid UserNetId { get; }
}