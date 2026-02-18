using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class AddNewProductIncomeFromActReconciliationItemMessage {
    public AddNewProductIncomeFromActReconciliationItemMessage(
        Guid itemNetId,
        Guid storageNetId,
        Guid userNetId,
        string comment,
        double qty,
        DateTime fromDate,
        string rowNumber,
        string storageNumber,
        string cellNumber
    ) {
        ItemNetId = itemNetId;

        StorageNetId = storageNetId;

        UserNetId = userNetId;

        Comment = comment;

        Qty = qty;

        FromDate = fromDate;

        RowNumber = rowNumber;

        StorageNumber = storageNumber;

        CellNumber = cellNumber;
    }

    public Guid ItemNetId { get; }

    public Guid StorageNetId { get; }

    public Guid UserNetId { get; }

    public string Comment { get; }

    public double Qty { get; }

    public DateTime FromDate { get; }

    public string RowNumber { get; }

    public string StorageNumber { get; }

    public string CellNumber { get; }
}