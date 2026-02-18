using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class GetAllVehiclesRangedMessage {
    public GetAllVehiclesRangedMessage(DateTime? from, DateTime? to) {
        From = from;
        To = to;
    }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}