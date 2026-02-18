using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class DynamicProductPlacementRowMap : EntityBaseMap<DynamicProductPlacementRow> {
    public override void Map(EntityTypeBuilder<DynamicProductPlacementRow> entity) {
        base.Map(entity);

        entity.ToTable("DynamicProductPlacementRow");

        entity.Property(e => e.DynamicProductPlacementColumnId).HasColumnName("DynamicProductPlacementColumnID");

        entity.Property(e => e.SupplyOrderUkraineItemId).HasColumnName("SupplyOrderUkraineItemID");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");

        entity.HasOne(e => e.DynamicProductPlacementColumn)
            .WithMany(e => e.DynamicProductPlacementRows)
            .HasForeignKey(e => e.DynamicProductPlacementColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraineItem)
            .WithMany(e => e.DynamicProductPlacementRows)
            .HasForeignKey(e => e.SupplyOrderUkraineItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.DynamicProductPlacementRows)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}