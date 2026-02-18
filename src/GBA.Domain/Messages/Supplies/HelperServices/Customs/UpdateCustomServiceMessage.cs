using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Customs;

public sealed class UpdateCustomServiceMessage {
    public UpdateCustomServiceMessage(Guid netId, CustomService customService) {
        NetId = netId;
        CustomService = customService;
    }

    public Guid NetId { get; set; }

    public CustomService CustomService { get; set; }
}