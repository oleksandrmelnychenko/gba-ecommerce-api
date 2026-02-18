using System;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Messages.Storages;

public sealed class GetAllStoragesForReturnsFilteredMessage {
    public GetAllStoragesForReturnsFilteredMessage(
        Guid organizationNetId,
        SaleReturnItemStatus status,
        Guid? orderItemNetId) {
        OrganizationNetId = organizationNetId;

        Status = status;
        OrderItemNetId = orderItemNetId;
    }

    public Guid OrganizationNetId { get; }

    public SaleReturnItemStatus Status { get; }

    public Guid? OrderItemNetId { get; }
}