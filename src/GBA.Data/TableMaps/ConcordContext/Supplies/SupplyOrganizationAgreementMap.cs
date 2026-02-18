using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrganizationAgreementMap : EntityBaseMap<SupplyOrganizationAgreement> {
    public override void Map(EntityTypeBuilder<SupplyOrganizationAgreement> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrganizationAgreement");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.CurrentAmount).HasColumnType("money");

        entity.Property(e => e.AccountingCurrentAmount).HasColumnType("money").HasDefaultValueSql("0.00");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Ignore(e => e.CurrentEuroAmount);

        entity.Property(e => e.TaxAccountingSchemeId).HasColumnName("TaxAccountingSchemeID");

        entity.Property(e => e.AgreementTypeCivilCodeId).HasColumnName("AgreementTypeCivilCodeID");

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID");

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID");

        entity.Property(e => e.SourceAmgId).HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasMaxLength(16);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.HasOne(e => e.TaxAccountingScheme)
            .WithMany(e => e.SupplyOrganizationAgreements)
            .HasForeignKey(e => e.TaxAccountingSchemeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AgreementTypeCivilCode)
            .WithMany(e => e.SupplyOrganizationAgreements)
            .HasForeignKey(e => e.AgreementTypeCivilCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.SupplyOrganizationAgreements)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}