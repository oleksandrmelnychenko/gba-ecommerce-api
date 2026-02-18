using System.Collections.Generic;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class UpdateReSaleAvailabilityListMessage {
    public UpdateReSaleAvailabilityListMessage(
        List<ReSaleAvailabilityItemModel> reSaleAvailabilityItemModels) {
        ReSaleAvailabilityItemModels = reSaleAvailabilityItemModels;
    }

    public List<ReSaleAvailabilityItemModel> ReSaleAvailabilityItemModels { get; set; }
}