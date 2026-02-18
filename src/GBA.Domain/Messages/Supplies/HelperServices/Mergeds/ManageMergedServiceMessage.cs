using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class ManageMergedServiceMessage {
    public ManageMergedServiceMessage(
        MergedService mergedService,
        Guid netId,
        Guid userNedId) {
        MergedService = mergedService;
        NetId = netId;
        UserNetId = userNedId;
    }

    public MergedService MergedService { get; }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}