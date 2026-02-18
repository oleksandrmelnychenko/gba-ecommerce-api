using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class StorageMap : EntityBaseMap<Storage> {
    public override void Map(EntityTypeBuilder<Storage> entity) {
        base.Map(entity);

        entity.ToTable("Storage");

        entity.Property(e => e.Name).HasMaxLength(40);

        entity.Property(e => e.Locale).HasMaxLength(10);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ForDefective).HasDefaultValueSql("0");

        entity.Property(e => e.IsResale).HasDefaultValueSql("0");

        entity.Property(e => e.ForVatProducts).HasDefaultValueSql("0");

        entity.Property(e => e.AvailableForReSale).HasDefaultValueSql("0");

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.Storages)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}