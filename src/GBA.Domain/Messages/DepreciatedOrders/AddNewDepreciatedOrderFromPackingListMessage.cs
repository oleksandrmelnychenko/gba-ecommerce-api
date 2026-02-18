using System;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class AddNewDepreciatedOrderFromPackingListMessage {
    public AddNewDepreciatedOrderFromPackingListMessage(
        PackingList packingList,
        DateTime fromDate,
        Guid organizationNetId,
        Guid userNetId
    ) {
        PackingList = packingList;

        FromDate = fromDate;

        OrganizationNetId = organizationNetId;

        UserNetId = userNetId;
    }

    public PackingList PackingList { get; }

    public DateTime FromDate { get; }

    public Guid OrganizationNetId { get; }

    public Guid UserNetId { get; }
}