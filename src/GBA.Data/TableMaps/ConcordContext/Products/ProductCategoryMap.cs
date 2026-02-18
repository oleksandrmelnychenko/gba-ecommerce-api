using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductCategoryMap : EntityBaseMap<ProductCategory> {
    public override void Map(EntityTypeBuilder<ProductCategory> entity) {
        base.Map(entity);

        entity.ToTable("ProductCategory");

        entity.Property(e => e.CategoryId).HasColumnName("CategoryID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.HasOne(e => e.Category)
            .WithMany(e => e.ProductCategories)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductCategories)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}