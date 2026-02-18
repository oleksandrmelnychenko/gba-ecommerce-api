using System;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class AddNewProductIncomeFromPackingListDynamicPlacementsMessage {
    public AddNewProductIncomeFromPackingListDynamicPlacementsMessage(PackingList packingList, Guid storageNetId, Guid userNetId, DateTime fromDate) {
        PackingList = packingList;

        StorageNetId = storageNetId;

        UserNetId = userNetId;

        FromDate = fromDate;
    }

    public PackingList PackingList { get; }

    public Guid StorageNetId { get; }

    public Guid UserNetId { get; }

    public DateTime FromDate { get; }
}