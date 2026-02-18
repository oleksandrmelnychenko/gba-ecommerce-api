using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductProductGroupMap : EntityBaseMap<ProductProductGroup> {
    public override void Map(EntityTypeBuilder<ProductProductGroup> entity) {
        base.Map(entity);

        entity.ToTable("ProductProductGroup");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");

        entity.Property(e => e.VendorCode).HasMaxLength(50);

        entity.Ignore(e => e.ProductGroups);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductProductGroups)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductGroup)
            .WithMany(e => e.ProductProductGroups)
            .HasForeignKey(e => e.ProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.Deleted, e.ProductId });

        entity.HasIndex(e => new { e.Deleted, e.ProductGroupId });

        entity.HasIndex(e => new { e.Deleted, e.ProductId, e.ProductGroupId });
    }
}