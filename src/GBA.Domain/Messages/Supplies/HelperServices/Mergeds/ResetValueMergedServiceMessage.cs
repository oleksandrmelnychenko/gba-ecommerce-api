using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class ResetValueMergedServiceMessage {
    public ResetValueMergedServiceMessage(
        long serviceId,
        Guid userNetId) : this() {
        ServiceId = serviceId;
        UserNetId = userNetId;
    }

    public ResetValueMergedServiceMessage(
        long serviceId,
        Guid userNetId,
        IEnumerable<long> invoiceIds) : this() {
        ServiceId = serviceId;
        UserNetId = userNetId;
        InvoiceIds = invoiceIds;
    }

    public ResetValueMergedServiceMessage(
        IEnumerable<long> serviceIds,
        Guid userNetId) : this() {
        ServiceIds = serviceIds;
        UserNetId = userNetId;
    }

    private ResetValueMergedServiceMessage() {
        ServiceIds = new List<long>();
        InvoiceIds = new List<long>();
    }

    public IEnumerable<long> ServiceIds { get; }

    public long ServiceId { get; }
    public Guid UserNetId { get; }

    public IEnumerable<long> InvoiceIds { get; }
}