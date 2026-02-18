using GBA.Domain.Entities.Clients.OrganizationClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.OrganizationClients;

public sealed class OrganizationClientAgreementMap : EntityBaseMap<OrganizationClientAgreement> {
    public override void Map(EntityTypeBuilder<OrganizationClientAgreement> entity) {
        base.Map(entity);

        entity.ToTable("OrganizationClientAgreement");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.OrganizationClientId).HasColumnName("OrganizationClientID");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.TaxAccountingSchemeId).HasColumnName("TaxAccountingSchemeID");

        entity.Property(e => e.AgreementTypeCivilCodeId).HasColumnName("AgreementTypeCivilCodeID");

        entity.HasOne(e => e.OrganizationClient)
            .WithMany(e => e.OrganizationClientAgreements)
            .HasForeignKey(e => e.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.OrganizationClientAgreements)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxAccountingScheme)
            .WithMany(e => e.OrganizationClientAgreements)
            .HasForeignKey(e => e.TaxAccountingSchemeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AgreementTypeCivilCode)
            .WithMany(e => e.OrganizationClientAgreements)
            .HasForeignKey(e => e.AgreementTypeCivilCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}