using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class AddNewDepreciatedOrderFromActReconciliationItemsMessage {
    public AddNewDepreciatedOrderFromActReconciliationItemsMessage(
        IEnumerable<ActReconciliationItem> items,
        string comment,
        Guid storageNetId,
        Guid organizationNetId,
        Guid userNetId,
        DateTime fromDate
    ) {
        Items = items;

        Comment = comment;

        StorageNetId = storageNetId;

        OrganizationNetId = organizationNetId;

        UserNetId = userNetId;

        FromDate = fromDate;
    }

    public IEnumerable<ActReconciliationItem> Items { get; }

    public string Comment { get; }

    public Guid StorageNetId { get; }

    public Guid OrganizationNetId { get; }

    public Guid UserNetId { get; }

    public DateTime FromDate { get; }
}