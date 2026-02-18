using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class AddOrUpdateMergedServiceMessage {
    public AddOrUpdateMergedServiceMessage(Guid supplyOrderNetId, Guid userNetId, MergedService mergedService) {
        SupplyOrderNetId = supplyOrderNetId;

        UserNetId = userNetId;

        MergedService = mergedService;
    }

    public Guid SupplyOrderNetId { get; }

    public Guid UserNetId { get; }

    public MergedService MergedService { get; }
}