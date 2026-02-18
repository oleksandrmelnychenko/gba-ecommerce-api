using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class AddOrUpdateMergedServiceUkraineMessage {
    public AddOrUpdateMergedServiceUkraineMessage(Guid supplyOrderUkraineNetId, Guid userNetId, MergedService mergedService) {
        SupplyOrderUkraineNetId = supplyOrderUkraineNetId;

        UserNetId = userNetId;

        MergedService = mergedService;
    }

    public Guid SupplyOrderUkraineNetId { get; }

    public Guid UserNetId { get; }

    public MergedService MergedService { get; }
}