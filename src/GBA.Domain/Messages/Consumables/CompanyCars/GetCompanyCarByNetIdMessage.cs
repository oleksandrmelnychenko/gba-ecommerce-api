using System;

namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class GetCompanyCarByNetIdMessage {
    public GetCompanyCarByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}