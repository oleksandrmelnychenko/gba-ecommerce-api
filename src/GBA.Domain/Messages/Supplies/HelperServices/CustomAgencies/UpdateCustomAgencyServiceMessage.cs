using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.CustomAgencies;

public sealed class UpdateCustomAgencyServiceMessage {
    public UpdateCustomAgencyServiceMessage(Guid netId, CustomAgencyService customAgencyService) {
        NetId = netId;
        CustomAgencyService = customAgencyService;
    }

    public Guid NetId { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }
}