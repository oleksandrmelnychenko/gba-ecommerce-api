using System;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class GetAllUnpaidConsumablesOrdersMessage {
    public GetAllUnpaidConsumablesOrdersMessage(Guid organizationNetId) {
        OrganizationNetId = organizationNetId;
    }

    public Guid OrganizationNetId { get; set; }
}