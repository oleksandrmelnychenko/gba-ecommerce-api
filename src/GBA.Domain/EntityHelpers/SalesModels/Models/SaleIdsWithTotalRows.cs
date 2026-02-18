namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class SaleIdsWithTotalRows {
    public SaleIdsWithTotalRows() { }

    public SaleIdsWithTotalRows(long id, long totalRowsQty) {
        Id = id;
        TotalRowsQty = totalRowsQty;
    }

    public long Id { get; set; }
    public long TotalRowsQty { get; set; }
}