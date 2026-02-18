using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderItemMap : EntityBaseMap<SupplyOrderItem> {
    public override void Map(EntityTypeBuilder<SupplyOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderItem");

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.TotalAmount).HasColumnType("money");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.IsPacked).HasDefaultValueSql("0");

        entity.Ignore(e => e.IsUpdated);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SupplyOrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyOrderItems)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}