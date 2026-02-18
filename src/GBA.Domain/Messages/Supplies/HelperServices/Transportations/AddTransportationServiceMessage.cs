using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Transportations;

public sealed class AddTransportationServiceMessage {
    public AddTransportationServiceMessage(
        Guid netId,
        TransportationService transportationService,
        Guid userNetId) {
        NetId = netId;

        TransportationService = transportationService;

        UserNetId = userNetId;
    }

    public Guid NetId { get; set; }

    public TransportationService TransportationService { get; set; }

    public Guid UserNetId { get; }
}