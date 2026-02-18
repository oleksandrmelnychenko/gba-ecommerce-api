using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductOriginalNumberMap : EntityBaseMap<ProductOriginalNumber> {
    public override void Map(EntityTypeBuilder<ProductOriginalNumber> entity) {
        base.Map(entity);

        entity.ToTable("ProductOriginalNumber");

        entity.Property(e => e.OriginalNumberId).HasColumnName("OriginalNumberID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.HasOne(e => e.OriginalNumber)
            .WithMany(e => e.ProductOriginalNumbers)
            .HasForeignKey(e => e.OriginalNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductOriginalNumbers)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.Deleted, e.ProductId });

        entity.HasIndex(e => new { e.Deleted, e.OriginalNumberId });
    }
}