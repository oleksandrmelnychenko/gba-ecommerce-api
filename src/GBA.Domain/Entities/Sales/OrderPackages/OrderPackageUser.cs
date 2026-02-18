namespace GBA.Domain.Entities.Sales.OrderPackages;

public sealed class OrderPackageUser : EntityBase {
    public long UserId { get; set; }

    public long OrderPackageId { get; set; }

    public User User { get; set; }

    public OrderPackage OrderPackage { get; set; }
}