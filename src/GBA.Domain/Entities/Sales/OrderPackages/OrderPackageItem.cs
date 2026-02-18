namespace GBA.Domain.Entities.Sales.OrderPackages;

public sealed class OrderPackageItem : EntityBase {
    public double Qty { get; set; }

    public long OrderItemId { get; set; }

    public long OrderPackageId { get; set; }

    public OrderItem OrderItem { get; set; }

    public OrderPackage OrderPackage { get; set; }
}