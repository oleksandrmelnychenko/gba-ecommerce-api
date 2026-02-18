using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductSetMap : EntityBaseMap<ProductSet> {
    public override void Map(EntityTypeBuilder<ProductSet> entity) {
        base.Map(entity);

        entity.ToTable("ProductSet");

        entity.Property(e => e.BaseProductId).HasColumnName("BaseProductID");

        entity.Property(e => e.ComponentProductId).HasColumnName("ComponentProductID");

        entity.Property(e => e.SetComponentsQty).HasDefaultValue(1);

        entity.HasOne(e => e.BaseProduct)
            .WithMany(e => e.BaseSetProducts)
            .HasForeignKey(e => e.BaseProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ComponentProduct)
            .WithMany(e => e.ComponentProducts)
            .HasForeignKey(e => e.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.Deleted, e.BaseProductId });

        entity.HasIndex(e => new { e.Deleted, e.ComponentProductId });
    }
}