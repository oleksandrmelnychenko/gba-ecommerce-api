namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class DynamicProductPlacement : EntityBase {
    public bool IsApplied { get; set; }

    public double Qty { get; set; }

    public string StorageNumber { get; set; }

    public string RowNumber { get; set; }

    public string CellNumber { get; set; }

    public long DynamicProductPlacementRowId { get; set; }

    public DynamicProductPlacementRow DynamicProductPlacementRow { get; set; }
}