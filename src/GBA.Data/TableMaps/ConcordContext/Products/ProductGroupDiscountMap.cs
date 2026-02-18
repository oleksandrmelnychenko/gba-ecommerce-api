using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductGroupDiscountMap : EntityBaseMap<ProductGroupDiscount> {
    public override void Map(EntityTypeBuilder<ProductGroupDiscount> entity) {
        base.Map(entity);

        entity.ToTable("ProductGroupDiscount");

        entity.Property(e => e.IsActive).HasDefaultValueSql("1");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");

        entity.Ignore(e => e.SubProductGroupDiscounts);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.ProductGroupDiscounts)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductGroup)
            .WithMany(e => e.ProductGroupDiscounts)
            .HasForeignKey(e => e.ProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}