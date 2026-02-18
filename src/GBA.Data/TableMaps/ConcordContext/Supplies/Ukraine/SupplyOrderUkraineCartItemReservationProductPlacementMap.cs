using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservationProductPlacementMap : EntityBaseMap<SupplyOrderUkraineCartItemReservationProductPlacement> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraineCartItemReservationProductPlacement> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraineCartItemReservationProductPlacement");

        entity.Property(e => e.ProductPlacementId).HasColumnName("ProductPlacementID");

        entity.Property(e => e.SupplyOrderUkraineCartItemReservationId).HasColumnName("SupplyOrderUkraineCartItemReservationID");

        entity.HasOne(e => e.ProductPlacement)
            .WithMany(e => e.SupplyOrderUkraineCartItemReservationProductPlacements)
            .HasForeignKey(e => e.ProductPlacementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraineCartItemReservation)
            .WithMany(e => e.SupplyOrderUkraineCartItemReservationProductPlacements)
            .HasForeignKey(e => e.SupplyOrderUkraineCartItemReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}