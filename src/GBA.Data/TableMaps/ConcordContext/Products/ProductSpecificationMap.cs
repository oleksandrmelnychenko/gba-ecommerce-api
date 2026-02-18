using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductSpecificationMap : EntityBaseMap<ProductSpecification> {
    public override void Map(EntityTypeBuilder<ProductSpecification> entity) {
        base.Map(entity);

        entity.ToTable("ProductSpecification");

        entity.Property(e => e.AddedById).HasColumnName("AddedByID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.DutyPercent).HasColumnType("money");

        entity.Property(e => e.Name).HasMaxLength(500);

        entity.Property(e => e.SpecificationCode).HasMaxLength(100);

        entity.Property(e => e.Locale).HasMaxLength(4);

        entity.Ignore(e => e.OrderProductSpecification);

        entity.Ignore(e => e.Price);

        entity.Ignore(e => e.Qty);

        entity.HasOne(e => e.AddedBy)
            .WithMany(e => e.ProductSpecifications)
            .HasForeignKey(e => e.AddedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductSpecifications)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}