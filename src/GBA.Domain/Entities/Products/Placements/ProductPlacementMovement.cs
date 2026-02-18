namespace GBA.Domain.Entities.Products;

public sealed class ProductPlacementMovement : EntityBase {
    public double Qty { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public long FromProductPlacementId { get; set; }

    public long ToProductPlacementId { get; set; }

    public long ResponsibleId { get; set; }

    public ProductPlacement FromProductPlacement { get; set; }

    public ProductPlacement ToProductPlacement { get; set; }

    public User Responsible { get; set; }
}