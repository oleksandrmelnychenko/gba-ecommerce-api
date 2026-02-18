using System;

namespace GBA.Domain.Messages.Supplies.ActProvidingServices;

public sealed class GetActProvidingServiceByNetIdMessage {
    public GetActProvidingServiceByNetIdMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}