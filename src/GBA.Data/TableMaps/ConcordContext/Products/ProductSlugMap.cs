using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductSlugMap : EntityBaseMap<ProductSlug> {
    public override void Map(EntityTypeBuilder<ProductSlug> entity) {
        base.Map(entity);

        entity.ToTable("ProductSlug");

        entity.Property(e => e.Url).HasMaxLength(250);

        entity.Property(e => e.Locale).HasMaxLength(4);

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductSlugs)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}