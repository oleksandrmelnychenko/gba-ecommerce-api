using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies.HelperServices.Vehicles;

public sealed class UpdateVehicleExtraChargeMessage {
    public UpdateVehicleExtraChargeMessage(
        Guid netId,
        SupplyExtraChargeType supplyExtraChargeType,
        Guid userNetId) {
        NetId = netId;

        SupplyExtraChargeType = supplyExtraChargeType;
        UserNetId = userNetId;
    }

    public Guid NetId { get; set; }
    public SupplyExtraChargeType SupplyExtraChargeType { get; set; }
    public Guid UserNetId { get; }
}