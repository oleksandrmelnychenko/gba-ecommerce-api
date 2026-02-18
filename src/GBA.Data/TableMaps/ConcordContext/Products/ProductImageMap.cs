using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductImageMap : EntityBaseMap<ProductImage> {
    public override void Map(EntityTypeBuilder<ProductImage> entity) {
        base.Map(entity);

        entity.ToTable("ProductImage");

        entity.Property(e => e.ImageUrl).HasMaxLength(500);

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductImages)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}