using System;

namespace GBA.Domain.Messages.Consumables.CompanyCarRoadLists;

public sealed class GetCompanyCarRoadListByNetIdMessage {
    public GetCompanyCarRoadListByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}