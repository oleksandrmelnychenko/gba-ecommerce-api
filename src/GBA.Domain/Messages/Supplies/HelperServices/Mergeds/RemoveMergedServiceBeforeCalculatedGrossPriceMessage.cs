using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class RemoveMergedServiceBeforeCalculatedGrossPriceMessage {
    public RemoveMergedServiceBeforeCalculatedGrossPriceMessage(
        Guid netId,
        Guid userNetId) {
        NetId = netId;
        UserNetId = userNetId;
    }

    public Guid NetId { get; }
    public Guid UserNetId { get; }
}