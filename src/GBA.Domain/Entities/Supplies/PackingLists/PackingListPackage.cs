using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.PackingLists;

public sealed class PackingListPackage : EntityBase {
    public PackingListPackage() {
        PackingListPackageOrderItems = new HashSet<PackingListPackageOrderItem>();
    }

    public double GrossWeight { get; set; }

    public double NetWeight { get; set; }

    public double CBM { get; set; }

    public PackingListPackageType Type { get; set; }

    public int Lenght { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public long PackingListId { get; set; }

    public PackingList PackingList { get; set; }

    public ICollection<PackingListPackageOrderItem> PackingListPackageOrderItems { get; set; }
}