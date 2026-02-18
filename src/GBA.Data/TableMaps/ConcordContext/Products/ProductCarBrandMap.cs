using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductCarBrandMap : EntityBaseMap<ProductCarBrand> {
    public override void Map(EntityTypeBuilder<ProductCarBrand> entity) {
        base.Map(entity);

        entity.ToTable("ProductCarBrand");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.CarBrandId).HasColumnName("CarBrandID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductCarBrands)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CarBrand)
            .WithMany(e => e.ProductCarBrands)
            .HasForeignKey(e => e.CarBrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}