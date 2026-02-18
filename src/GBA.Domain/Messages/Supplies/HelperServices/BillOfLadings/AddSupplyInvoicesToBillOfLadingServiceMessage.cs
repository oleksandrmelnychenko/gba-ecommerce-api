using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;

public sealed class AddSupplyInvoicesToBillOfLadingServiceMessage {
    public AddSupplyInvoicesToBillOfLadingServiceMessage(
        BillOfLadingService service,
        Guid userNetId) {
        Service = service;
        UserNetId = userNetId;
    }

    public BillOfLadingService Service { get; }
    public Guid UserNetId { get; }
}