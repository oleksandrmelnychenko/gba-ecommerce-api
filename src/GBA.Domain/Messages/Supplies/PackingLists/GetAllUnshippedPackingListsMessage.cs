using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class GetAllUnshippedPackingListsMessage {
    public GetAllUnshippedPackingListsMessage(
        SupplyTransportationType supplyTransportationType,
        Guid organizationNetId) {
        SupplyTransportationType = supplyTransportationType;

        OrganizationNetId = organizationNetId;
    }

    public SupplyTransportationType SupplyTransportationType { get; }

    public Guid OrganizationNetId { get; }
}