using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class AddSupplyInvoicesToMergedServiceMessage {
    public AddSupplyInvoicesToMergedServiceMessage(
        MergedService service,
        Guid userNetId) {
        Service = service;
        UserNetId = userNetId;
    }

    public MergedService Service { get; }
    public Guid UserNetId { get; }
}