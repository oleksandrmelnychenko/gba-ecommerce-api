using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;

public sealed class ResetValueBillOfLadingServiceMessage {
    public ResetValueBillOfLadingServiceMessage(long serviceId,
        Guid userNetId) : this() {
        ServiceId = serviceId;
        UserNetId = userNetId;
    }

    public ResetValueBillOfLadingServiceMessage(long serviceId,
        Guid userNetId,
        IEnumerable<long> invoiceIds) {
        ServiceId = serviceId;
        UserNetId = userNetId;
        InvoiceIds = invoiceIds;
    }

    public ResetValueBillOfLadingServiceMessage() {
        InvoiceIds = new List<long>();
    }

    public long ServiceId { get; }
    public Guid UserNetId { get; }
    public IEnumerable<long> InvoiceIds { get; }
}