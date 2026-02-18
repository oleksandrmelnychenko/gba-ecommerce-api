using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class CompanyCarMap : EntityBaseMap<CompanyCar> {
    public override void Map(EntityTypeBuilder<CompanyCar> entity) {
        base.Map(entity);

        entity.ToTable("CompanyCar");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.ConsumablesStorageId).HasColumnName("ConsumablesStorageID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.LicensePlate).HasMaxLength(20);

        entity.Property(e => e.CarBrand).HasMaxLength(100);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedCompanyCars)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedCompanyCars)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumablesStorage)
            .WithOne(e => e.CompanyCar)
            .HasForeignKey<CompanyCar>(e => e.ConsumablesStorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.CompanyCars)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}