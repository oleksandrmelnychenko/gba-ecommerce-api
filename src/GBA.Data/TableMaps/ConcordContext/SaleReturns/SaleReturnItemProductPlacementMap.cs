using GBA.Domain.Entities.SaleReturns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.SaleReturns;

public sealed class SaleReturnItemProductPlacementMap : EntityBaseMap<SaleReturnItemProductPlacement> {
    public override void Map(EntityTypeBuilder<SaleReturnItemProductPlacement> entity) {
        base.Map(entity);

        entity.ToTable("SaleReturnItemProductPlacement");

        entity.Property(e => e.ProductPlacementId).HasColumnName("ProductPlacementID");

        entity.HasOne(e => e.SaleReturnItem)
            .WithMany(e => e.SaleReturnItemProductPlacements)
            .HasForeignKey(e => e.SaleReturnItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}