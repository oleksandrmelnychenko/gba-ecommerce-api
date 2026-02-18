using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class DynamicProductPlacementMap : EntityBaseMap<DynamicProductPlacement> {
    public override void Map(EntityTypeBuilder<DynamicProductPlacement> entity) {
        base.Map(entity);

        entity.ToTable("DynamicProductPlacement");

        entity.Property(e => e.DynamicProductPlacementRowId).HasColumnName("DynamicProductPlacementRowID");

        entity.Property(e => e.CellNumber).HasMaxLength(5);

        entity.Property(e => e.RowNumber).HasMaxLength(5);

        entity.Property(e => e.StorageNumber).HasMaxLength(5);

        entity.Property(e => e.IsApplied).HasDefaultValueSql("0");

        entity.HasOne(e => e.DynamicProductPlacementRow)
            .WithMany(e => e.DynamicProductPlacements)
            .HasForeignKey(e => e.DynamicProductPlacementRowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}