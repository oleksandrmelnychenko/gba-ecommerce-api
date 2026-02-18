using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class AddNewProductIncomeFromPackingListMessage {
    public AddNewProductIncomeFromPackingListMessage(Guid packingListNetId, Guid storageNetId, Guid userNetId, DateTime fromDate) {
        PackingListNetId = packingListNetId;

        StorageNetId = storageNetId;

        UserNetId = userNetId;

        FromDate = fromDate;
    }

    public Guid PackingListNetId { get; }

    public Guid StorageNetId { get; }

    public Guid UserNetId { get; }

    public DateTime FromDate { get; }
}