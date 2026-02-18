using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.PortCustomAgencies;

public sealed class InsertOrUpdatePortCustomAgencyServiceMessage {
    public InsertOrUpdatePortCustomAgencyServiceMessage(Guid netId, PortCustomAgencyService portCustomAgencyService) {
        NetId = netId;

        PortCustomAgencyService = portCustomAgencyService;
    }

    public Guid NetId { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }
}