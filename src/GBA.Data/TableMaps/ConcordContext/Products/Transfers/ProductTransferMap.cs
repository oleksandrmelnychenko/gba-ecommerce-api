using GBA.Domain.Entities.Products.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products.Transfers;

public sealed class ProductTransferMap : EntityBaseMap<ProductTransfer> {
    public override void Map(EntityTypeBuilder<ProductTransfer> entity) {
        base.Map(entity);

        entity.ToTable("ProductTransfer");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.FromStorageId).HasColumnName("FromStorageID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.ToStorageId).HasColumnName("ToStorageID");

        entity.Ignore(e => e.ExchangeRate);

        entity.HasOne(e => e.FromStorage)
            .WithMany(e => e.FromProductTransfers)
            .HasForeignKey(e => e.FromStorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.ProductTransfers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleProductTransfers)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ToStorage)
            .WithMany(e => e.ToProductTransfers)
            .HasForeignKey(e => e.ToStorageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}