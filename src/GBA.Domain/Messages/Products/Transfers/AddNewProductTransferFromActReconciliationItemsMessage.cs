using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class AddNewProductTransferFromActReconciliationItemsMessage {
    public AddNewProductTransferFromActReconciliationItemsMessage(
        IEnumerable<ActReconciliationItem> items,
        Guid fromStorageNetId,
        Guid toStorageNetId,
        Guid organizationNetId,
        Guid userNetId,
        string comment,
        DateTime fromDate
    ) {
        Items = items;

        FromStorageNetId = fromStorageNetId;

        ToStorageNetId = toStorageNetId;

        OrganizationNetId = organizationNetId;

        UserNetId = userNetId;

        Comment = comment;

        FromDate = fromDate;
    }

    public IEnumerable<ActReconciliationItem> Items { get; }

    public Guid FromStorageNetId { get; }

    public Guid ToStorageNetId { get; }

    public Guid OrganizationNetId { get; }

    public Guid UserNetId { get; }

    public string Comment { get; }

    public DateTime FromDate { get; }
}