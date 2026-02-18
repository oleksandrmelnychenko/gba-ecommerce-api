using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductLocationHistoryMap : EntityBaseMap<ProductLocationHistory> {
    public override void Map(EntityTypeBuilder<ProductLocationHistory> entity) {
        base.Map(entity);

        entity.ToTable("ProductLocationHistory");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.ProductPlacementId).HasColumnName("ProductPlacementID");

        entity.Property(e => e.HistoryInvoiceEditId).HasColumnName("HistoryInvoiceEditID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.DepreciatedOrderItemId).HasColumnName("DepreciatedOrderItemID");


        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ProductLocationsHistory)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductPlacement)
            .WithMany(e => e.ProductLocationsHistory)
            .HasForeignKey(e => e.ProductPlacementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductLocationsHistory)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedOrderItem)
            .WithMany(e => e.ProductLocationsHistory)
            .HasForeignKey(e => e.DepreciatedOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}