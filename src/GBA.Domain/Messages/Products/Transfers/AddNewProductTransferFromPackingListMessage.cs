using System;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class AddNewProductTransferFromPackingListMessage {
    public AddNewProductTransferFromPackingListMessage(
        PackingList packingList,
        DateTime fromDate,
        Guid organizationNetId,
        Guid toStorageNetId,
        Guid userNetId,
        string storageNumber,
        string rowNumber,
        string cellNumber
    ) {
        PackingList = packingList;

        FromDate = fromDate;

        OrganizationNetId = organizationNetId;

        ToStorageNetId = toStorageNetId;

        UserNetId = userNetId;

        StorageNumber = storageNumber;

        RowNumber = rowNumber;

        CellNumber = cellNumber;
    }

    public PackingList PackingList { get; }

    public DateTime FromDate { get; }

    public Guid OrganizationNetId { get; }

    public Guid ToStorageNetId { get; }

    public Guid UserNetId { get; }

    public string StorageNumber { get; }

    public string RowNumber { get; }

    public string CellNumber { get; }
}