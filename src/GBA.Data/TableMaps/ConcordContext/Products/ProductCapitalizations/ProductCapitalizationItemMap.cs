using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductCapitalizationItemMap : EntityBaseMap<ProductCapitalizationItem> {
    public override void Map(EntityTypeBuilder<ProductCapitalizationItem> entity) {
        base.Map(entity);

        entity.ToTable("ProductCapitalizationItem");

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ProductCapitalizationId).HasColumnName("ProductCapitalizationID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalNetWeight);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductCapitalizationItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductCapitalization)
            .WithMany(e => e.ProductCapitalizationItems)
            .HasForeignKey(e => e.ProductCapitalizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}