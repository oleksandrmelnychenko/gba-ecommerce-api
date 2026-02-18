using System.Collections.Generic;

namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentFromProductTransferWithReSaleMessage {
    public StoreConsignmentFromProductTransferWithReSaleMessage(
        long productTransferId,
        Dictionary<long, long> productTransferItemProductAvailability,
        bool withReSale,
        bool isFile = false,
        string rowNumber = null,
        string cellNumber = null,
        string storageNumber = null,
        long userId = 0) {
        ProductTransferId = productTransferId;
        ProductTransferItemProductAvailability = productTransferItemProductAvailability;
        WithReSale = withReSale;
        IsFile = isFile;
        RowNumber = rowNumber;
        CellNumber = cellNumber;
        StorageNumber = storageNumber;
        UserId = userId;
    }

    public long ProductTransferId { get; }
    public Dictionary<long, long> ProductTransferItemProductAvailability { get; }
    public bool WithReSale { get; }
    public bool IsFile { get; }
    public string RowNumber { get; }
    public string CellNumber { get; }
    public string StorageNumber { get; }
    public long UserId { get; }
}