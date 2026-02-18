using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class AgreementMap : EntityBaseMap<Agreement> {
    public override void Map(EntityTypeBuilder<Agreement> entity) {
        base.Map(entity);

        entity.ToTable("Agreement");

        entity.Property(e => e.AmountDebt).HasColumnType("money");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.PricingId).HasColumnName("PricingID");

        entity.Property(e => e.ProviderPricingId).HasColumnName("ProviderPricingID");

        entity.Property(e => e.TaxAccountingSchemeId).HasColumnName("TaxAccountingSchemeID");

        entity.Property(e => e.AgreementTypeCivilCodeId).HasColumnName("AgreementTypeCivilCodeID");

        entity.Property(e => e.PromotionalPricingId).HasColumnName("PromotionalPricingID");

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID");

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID");

        entity.Property(e => e.SourceAmgId).HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasMaxLength(16);

        entity.Ignore(e => e.ExpiredDays);

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.PricingId);

        entity.HasOne(e => e.PromotionalPricing)
            .WithMany(e => e.PromotionalAgreements)
            .HasForeignKey(e => e.PromotionalPricingId);

        entity.HasOne(e => e.ProviderPricing)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.ProviderPricingId);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.CurrencyId);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.OrganizationId);

        entity.HasOne(e => e.TaxAccountingScheme)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.TaxAccountingSchemeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AgreementTypeCivilCode)
            .WithMany(e => e.Agreements)
            .HasForeignKey(e => e.AgreementTypeCivilCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}