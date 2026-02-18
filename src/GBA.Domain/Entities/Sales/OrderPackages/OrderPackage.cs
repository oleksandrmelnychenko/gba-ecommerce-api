using System.Collections.Generic;

namespace GBA.Domain.Entities.Sales.OrderPackages;

public sealed class OrderPackage : EntityBase {
    public OrderPackage() {
        OrderPackageUsers = new HashSet<OrderPackageUser>();

        OrderPackageItems = new HashSet<OrderPackageItem>();
    }

    public double CBM { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Lenght { get; set; }

    public double Weight { get; set; }

    public long OrderId { get; set; }

    public Order Order { get; set; }

    public ICollection<OrderPackageUser> OrderPackageUsers { get; set; }

    public ICollection<OrderPackageItem> OrderPackageItems { get; set; }
}