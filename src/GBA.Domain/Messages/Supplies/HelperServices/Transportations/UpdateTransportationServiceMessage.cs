using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Transportations;

public sealed class UpdateTransportationServiceMessage {
    public UpdateTransportationServiceMessage(Guid netId, TransportationService transportationService) {
        NetId = netId;
        TransportationService = transportationService;
    }

    public Guid NetId { get; set; }

    public TransportationService TransportationService { get; set; }
}