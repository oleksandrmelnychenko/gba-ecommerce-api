using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductReservationMap : EntityBaseMap<ProductReservation> {
    public override void Map(EntityTypeBuilder<ProductReservation> entity) {
        base.Map(entity);

        entity.ToTable("ProductReservation");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.ProductAvailabilityId).HasColumnName("ProductAvailabilityID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Property(e => e.IsReSaleReservation).HasDefaultValueSql("0");

        entity.Ignore(e => e.RegionCode);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ProductReservations)
            .HasForeignKey(e => e.OrderItemId);

        entity.HasOne(e => e.ProductAvailability)
            .WithMany(e => e.ProductReservations)
            .HasForeignKey(e => e.ProductAvailabilityId);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.ProductReservations)
            .HasForeignKey(e => e.ConsignmentItemId);
    }
}