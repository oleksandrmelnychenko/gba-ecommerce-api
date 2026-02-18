using System;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class AddNewProductTransferMessage {
    public AddNewProductTransferMessage(
        ProductTransfer productTransfer,
        Guid userNetId,
        string storageNumber,
        string rowNumber,
        string cellNumber
    ) {
        ProductTransfer = productTransfer;

        UserNetId = userNetId;

        StorageNumber = storageNumber;

        RowNumber = rowNumber;

        CellNumber = cellNumber;
    }

    public ProductTransfer ProductTransfer { get; }

    public Guid UserNetId { get; }

    public string StorageNumber { get; }

    public string RowNumber { get; }

    public string CellNumber { get; }
}