using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductPlacementMap : EntityBaseMap<ProductPlacement> {
    public override void Map(EntityTypeBuilder<ProductPlacement> entity) {
        base.Map(entity);

        entity.ToTable("ProductPlacement");

        entity.Property(e => e.CellNumber).HasMaxLength(5);

        entity.Property(e => e.RowNumber).HasMaxLength(5);

        entity.Property(e => e.StorageNumber).HasMaxLength(5);

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");

        entity.Property(e => e.SupplyOrderUkraineItemId).HasColumnName("SupplyOrderUkraineItemID");

        entity.Property(e => e.ProductIncomeItemId).HasColumnName("ProductIncomeItemID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraineItem)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.SupplyOrderUkraineItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductIncomeItem)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.ProductIncomeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.ProductPlacements)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}