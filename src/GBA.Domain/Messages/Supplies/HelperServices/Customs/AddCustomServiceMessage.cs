using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Customs;

public sealed class AddCustomServiceMessage {
    public AddCustomServiceMessage(Guid supplyOrderNetId, CustomService customService) {
        SupplyOrderNetId = supplyOrderNetId;
        CustomService = customService;
    }

    public Guid SupplyOrderNetId { get; set; }

    public CustomService CustomService { get; set; }
}