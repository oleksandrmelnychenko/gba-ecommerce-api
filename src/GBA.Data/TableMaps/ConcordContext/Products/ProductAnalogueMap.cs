using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductAnalogueMap : EntityBaseMap<ProductAnalogue> {
    public override void Map(EntityTypeBuilder<ProductAnalogue> entity) {
        base.Map(entity);

        entity.ToTable("ProductAnalogue");

        entity.Property(e => e.BaseProductId).HasColumnName("BaseProductID");

        entity.Property(e => e.AnalogueProductId).HasColumnName("AnalogueProductID");

        entity.HasOne(e => e.AnalogueProduct)
            .WithMany(e => e.AnalogueProducts)
            .HasForeignKey(e => e.AnalogueProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BaseProduct)
            .WithMany(e => e.BaseAnalogueProducts)
            .HasForeignKey(e => e.BaseProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.Deleted, e.BaseProductId });

        entity.HasIndex(e => new { e.Deleted, e.AnalogueProductId });
    }
}