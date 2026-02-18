using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Organizations;

public sealed class OrganizationMap : EntityBaseMap<Organization> {
    public override void Map(EntityTypeBuilder<Organization> entity) {
        base.Map(entity);

        entity.ToTable("Organization");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.Property(e => e.FullName).HasMaxLength(150);

        entity.Property(e => e.Code).HasMaxLength(5);

        entity.Property(e => e.TIN).HasMaxLength(100);

        entity.Property(e => e.USREOU).HasMaxLength(100);

        entity.Property(e => e.SROI).HasMaxLength(150);

        entity.Property(e => e.RegistrationNumber).HasMaxLength(150);

        entity.Property(e => e.PFURegistrationNumber).HasMaxLength(150);

        entity.Property(e => e.PhoneNumber).HasMaxLength(150);

        entity.Property(e => e.Address).HasMaxLength(250);

        entity.Property(e => e.IsIndividual).HasDefaultValueSql("0");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.TaxInspectionId).HasColumnName("TaxInspectionID");

        entity.Property(e => e.TypeTaxation).HasDefaultValueSql("0");

        entity.Property(e => e.Manager).HasMaxLength(200);

        entity.Property(e => e.VatRateId).HasColumnName("VatRateID");

        entity.Ignore(e => e.NameUk);

        entity.Ignore(e => e.NamePl);

        entity.Ignore(e => e.MainPaymentRegister);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.Organizations)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.Organizations)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxInspection)
            .WithMany(e => e.Organizations)
            .HasForeignKey(e => e.TaxInspectionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.VatRate)
            .WithMany(e => e.Organizations)
            .HasForeignKey(e => e.VatRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}