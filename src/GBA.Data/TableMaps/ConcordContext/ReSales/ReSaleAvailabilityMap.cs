using GBA.Domain.Entities.ReSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ReSales;

public class ReSaleAvailabilityMap : EntityBaseMap<ReSaleAvailability> {
    public override void Map(EntityTypeBuilder<ReSaleAvailability> entity) {
        base.Map(entity);

        entity.ToTable("ReSaleAvailability");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Property(e => e.ProductAvailabilityId).HasColumnName("ProductAvailabilityID");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.ProductTransferItemId).HasColumnName("ProductTransferItemID");

        entity.Property(e => e.DepreciatedOrderItemId).HasColumnName("DepreciatedOrderItemID");

        entity.Property(e => e.ProductReservationId).HasColumnName("ProductReservationID");

        entity.Property(e => e.PricePerItem).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Property(e => e.InvoiceQty).HasColumnName("InvoiceQty");

        entity.Ignore(e => e.TotalPrice);

        entity.Ignore(e => e.PricePerItemWithExtraCharge);

        entity.Ignore(e => e.TotalPriceWithExtraCharge);

        entity.Ignore(e => e.IsSelected);

        entity.Ignore(e => e.Product);

        entity.Ignore(e => e.QtyToReSale);

        entity.Ignore(e => e.ProductLocations);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductAvailability)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.ProductAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductReservation)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.ProductReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductTransferItem)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.ProductTransferItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedOrderItem)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.DepreciatedOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyReturnItem)
            .WithMany(e => e.ReSaleAvailabilities)
            .HasForeignKey(e => e.SupplyReturnItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}