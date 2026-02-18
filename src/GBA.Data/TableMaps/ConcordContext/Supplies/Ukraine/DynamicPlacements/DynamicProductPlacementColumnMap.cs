using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class DynamicProductPlacementColumnMap : EntityBaseMap<DynamicProductPlacementColumn> {
    public override void Map(EntityTypeBuilder<DynamicProductPlacementColumn> entity) {
        base.Map(entity);

        entity.ToTable("DynamicProductPlacementColumn");

        entity.Property(e => e.PackingListId).HasColumnName("PackingListID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.HasOne(e => e.PackingList)
            .WithMany(e => e.DynamicProductPlacementColumns)
            .HasForeignKey(e => e.PackingListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.DynamicProductPlacementColumns)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}