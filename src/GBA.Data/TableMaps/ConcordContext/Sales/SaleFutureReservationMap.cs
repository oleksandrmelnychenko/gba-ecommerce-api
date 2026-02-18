using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleFutureReservationMap : EntityBaseMap<SaleFutureReservation> {
    public override void Map(EntityTypeBuilder<SaleFutureReservation> entity) {
        base.Map(entity);

        entity.ToTable("SaleFutureReservation");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.SaleFutureReservations)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SaleFutureReservations)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SaleFutureReservations)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}