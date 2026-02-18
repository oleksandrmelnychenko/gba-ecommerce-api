using GBA.Domain.Entities.Sales.OrderPackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales.OrderPackages;

public sealed class OrderPackageItemMap : EntityBaseMap<OrderPackageItem> {
    public override void Map(EntityTypeBuilder<OrderPackageItem> entity) {
        base.Map(entity);

        entity.ToTable("OrderPackageItem");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.OrderPackageId).HasColumnName("OrderPackageID");

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.OrderPackageItems)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderPackage)
            .WithMany(e => e.OrderPackageItems)
            .HasForeignKey(e => e.OrderPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}