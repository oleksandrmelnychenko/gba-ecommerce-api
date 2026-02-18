using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductLocationMap : EntityBaseMap<ProductLocation> {
    public override void Map(EntityTypeBuilder<ProductLocation> entity) {
        base.Map(entity);

        entity.ToTable("ProductLocation");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.ProductPlacementId).HasColumnName("ProductPlacementID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.DepreciatedOrderItemId).HasColumnName("DepreciatedOrderItemID");

        entity.Property(e => e.ProductTransferItemId).HasColumnName("ProductTransferItemID");

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ProductLocations)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductPlacement)
            .WithMany(e => e.ProductLocations)
            .HasForeignKey(e => e.ProductPlacementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductLocations)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedOrderItem)
            .WithMany(e => e.ProductLocations)
            .HasForeignKey(e => e.DepreciatedOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductTransferItem)
            .WithMany(e => e.ProductLocations)
            .HasForeignKey(e => e.ProductTransferItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}