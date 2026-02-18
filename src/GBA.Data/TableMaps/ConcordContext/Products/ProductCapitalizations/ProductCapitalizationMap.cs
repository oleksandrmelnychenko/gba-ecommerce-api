using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductCapitalizationMap : EntityBaseMap<ProductCapitalization> {
    public override void Map(EntityTypeBuilder<ProductCapitalization> entity) {
        base.Map(entity);

        entity.ToTable("ProductCapitalization");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.ExchangeRate);

        entity.Ignore(e => e.Currency);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.ProductCapitalizations)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ProductCapitalizations)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductCapitalizations)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}