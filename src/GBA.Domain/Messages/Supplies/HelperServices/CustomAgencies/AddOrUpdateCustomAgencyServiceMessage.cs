using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.CustomAgencies;

public sealed class AddOrUpdateCustomAgencyServiceMessage {
    public AddOrUpdateCustomAgencyServiceMessage(Guid netId, CustomAgencyService customAgencyService) {
        CustomAgencyService = customAgencyService;
        NetId = netId;
    }

    public Guid NetId { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }
}