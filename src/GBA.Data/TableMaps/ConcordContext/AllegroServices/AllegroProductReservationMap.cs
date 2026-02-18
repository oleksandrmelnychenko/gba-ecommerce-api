using GBA.Domain.Entities.AllegroServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.AllegroServices;

public sealed class AllegroProductReservationMap : EntityBaseMap<AllegroProductReservation> {
    public override void Map(EntityTypeBuilder<AllegroProductReservation> entity) {
        base.Map(entity);

        entity.ToTable("AllegroProductReservation");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.AllegroItemId).HasColumnName("AllegroItemID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.AllegroProductReservations)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}