namespace GBA.Domain.Entities.Sales.Shipments;

public sealed class ShipmentListItem : EntityBase {
    public string Comment { get; set; }

    public double QtyPlaces { get; set; }

    public long SaleId { get; set; }

    public long ShipmentListId { get; set; }

    public Sale Sale { get; set; }

    public ShipmentList ShipmentList { get; set; }

    public bool IsChangeTransporter { get; set; }
}