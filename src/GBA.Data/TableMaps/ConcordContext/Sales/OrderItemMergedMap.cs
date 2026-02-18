using GBA.Domain.Entities.Sales.SaleMerges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class OrderItemMergedMap : EntityBaseMap<OrderItemMerged> {
    public override void Map(EntityTypeBuilder<OrderItemMerged> entity) {
        base.Map(entity);

        entity.ToTable("OrderItemMerged");

        entity.Property(e => e.OldOrderId).HasColumnName("OldOrderID");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.OldOrderItemId).HasColumnName("OldOrderItemID");

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.OrderItemMerges)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OldOrderItem)
            .WithMany(e => e.OldOrderItemMerges)
            .HasForeignKey(e => e.OldOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OldOrder)
            .WithMany(e => e.OrderItemMerges)
            .HasForeignKey(e => e.OldOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}