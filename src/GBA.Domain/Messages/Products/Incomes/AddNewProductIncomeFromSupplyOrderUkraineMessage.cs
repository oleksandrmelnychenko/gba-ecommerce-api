using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class AddNewProductIncomeFromSupplyOrderUkraineMessage {
    public AddNewProductIncomeFromSupplyOrderUkraineMessage(
        SupplyOrderUkraine supplyOrderUkraine,
        Guid storageNetId,
        Guid userNetId,
        DateTime fromDate
    ) {
        SupplyOrderUkraine = supplyOrderUkraine;

        StorageNetId = storageNetId;

        UserNetId = userNetId;

        FromDate = fromDate;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public Guid StorageNetId { get; }

    public Guid UserNetId { get; }

    public DateTime FromDate { get; }
}