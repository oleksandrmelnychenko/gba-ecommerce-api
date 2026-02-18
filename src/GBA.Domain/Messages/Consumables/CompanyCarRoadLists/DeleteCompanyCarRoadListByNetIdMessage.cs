using System;

namespace GBA.Domain.Messages.Consumables.CompanyCarRoadLists;

public sealed class DeleteCompanyCarRoadListByNetIdMessage {
    public DeleteCompanyCarRoadListByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}