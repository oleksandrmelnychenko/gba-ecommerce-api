using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservationMap : EntityBaseMap<SupplyOrderUkraineCartItemReservation> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraineCartItemReservation> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraineCartItemReservation");

        entity.Property(e => e.ProductAvailabilityId).HasColumnName("ProductAvailabilityID");

        entity.Property(e => e.SupplyOrderUkraineCartItemId).HasColumnName("SupplyOrderUkraineCartItemID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.HasOne(e => e.ProductAvailability)
            .WithMany(e => e.SupplyOrderUkraineCartItemReservations)
            .HasForeignKey(e => e.ProductAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraineCartItem)
            .WithMany(e => e.SupplyOrderUkraineCartItemReservations)
            .HasForeignKey(e => e.SupplyOrderUkraineCartItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.SupplyOrderUkraineCartItemReservations)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}