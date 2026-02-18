using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class AddNewProductIncomeFromActReconciliationItemsMessage {
    public AddNewProductIncomeFromActReconciliationItemsMessage(
        IEnumerable<ActReconciliationItem> items,
        Guid storageNetId,
        Guid userNetId,
        string comment,
        DateTime fromDate
    ) {
        Items = items;

        StorageNetId = storageNetId;

        UserNetId = userNetId;

        Comment = comment;

        FromDate = fromDate;
    }

    public IEnumerable<ActReconciliationItem> Items { get; }

    public Guid StorageNetId { get; }

    public Guid UserNetId { get; }

    public string Comment { get; }

    public DateTime FromDate { get; }
}