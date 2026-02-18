using System;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class AddNewProductTransferFromActReconciliationItemMessage {
    public AddNewProductTransferFromActReconciliationItemMessage(
        Guid itemNetId,
        Guid fromStorageNetId,
        Guid toStorageNetId,
        Guid organizationNetId,
        Guid userNetId,
        string comment,
        string reason,
        string storageNumber,
        string rowNumber,
        string cellNumber,
        double qty,
        DateTime fromDate
    ) {
        ItemNetId = itemNetId;

        FromStorageNetId = fromStorageNetId;

        ToStorageNetId = toStorageNetId;

        OrganizationNetId = organizationNetId;

        UserNetId = userNetId;

        Comment = comment;

        Reason = reason;

        StorageNumber = storageNumber;

        RowNumber = rowNumber;

        CellNumber = cellNumber;

        Qty = qty;

        FromDate = fromDate;
    }

    public Guid ItemNetId { get; }

    public Guid FromStorageNetId { get; }

    public Guid ToStorageNetId { get; }

    public Guid OrganizationNetId { get; }

    public Guid UserNetId { get; }

    public string Comment { get; }

    public string Reason { get; }

    public string StorageNumber { get; }

    public string RowNumber { get; }

    public string CellNumber { get; }

    public double Qty { get; set; }

    public DateTime FromDate { get; }
}