using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class GetAllInvoicesFromApprovedSupplyOrderMessage {
    public GetAllInvoicesFromApprovedSupplyOrderMessage(
        SupplyTransportationType supplyTransportationType,
        Guid organizationNetId,
        Guid netId) {
        SupplyTransportationType = supplyTransportationType;
        OrganizationNetId = organizationNetId;
        NetId = netId;
    }

    public SupplyTransportationType SupplyTransportationType { get; }

    public Guid OrganizationNetId { get; }

    public Guid NetId { get; }
}