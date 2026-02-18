using GBA.Domain.Entities.DepreciatedOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.DepreciatedOrders;

public sealed class DepreciatedOrderItemMap : EntityBaseMap<DepreciatedOrderItem> {
    public override void Map(EntityTypeBuilder<DepreciatedOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("DepreciatedOrderItem");

        entity.Property(e => e.Reason).HasMaxLength(150);

        entity.Property(e => e.DepreciatedOrderId).HasColumnName("DepreciatedOrderID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ActReconciliationItemId).HasColumnName("ActReconciliationItemID");

        entity.Ignore(e => e.PerUnitPrice);

        entity.HasOne(e => e.DepreciatedOrder)
            .WithMany(e => e.DepreciatedOrderItems)
            .HasForeignKey(e => e.DepreciatedOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.DepreciatedOrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActReconciliationItem)
            .WithMany(e => e.DepreciatedOrderItems)
            .HasForeignKey(e => e.ActReconciliationItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}