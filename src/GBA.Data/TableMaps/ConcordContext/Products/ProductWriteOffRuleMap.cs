using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductWriteOffRuleMap : EntityBaseMap<ProductWriteOffRule> {
    public override void Map(EntityTypeBuilder<ProductWriteOffRule> entity) {
        base.Map(entity);

        entity.ToTable("ProductWriteOffRule");

        entity.Property(e => e.RuleLocale).HasMaxLength(4);

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedProductWriteOffRules)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedProductWriteOffRules)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductWriteOffRules)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ProductGroup)
            .WithMany(e => e.ProductWriteOffRules)
            .HasForeignKey(e => e.ProductGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}