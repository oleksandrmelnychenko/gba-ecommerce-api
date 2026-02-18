using System;

namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class DeleteCompanyCarByNetIdMessage {
    public DeleteCompanyCarByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}