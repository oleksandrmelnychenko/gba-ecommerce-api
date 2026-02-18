using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductAvailabilityMap : EntityBaseMap<ProductAvailability> {
    public override void Map(EntityTypeBuilder<ProductAvailability> entity) {
        base.Map(entity);

        entity.ToTable("ProductAvailability");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductAvailabilities)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductAvailabilities)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(e => e.IsReSaleAvailability);

        entity.HasIndex(e => e.Amount).IsUnique(false);

        entity.HasIndex(e => new { e.Deleted, e.ProductId }).IsUnique(false);

        entity.HasIndex(e => new { e.Id, e.Deleted, e.ProductId });

        entity.HasIndex(e => new { e.Id, e.Deleted, e.StorageId });

        entity.HasIndex(e => new { e.StorageId, e.Amount, e.ProductId, e.Deleted });
    }
}