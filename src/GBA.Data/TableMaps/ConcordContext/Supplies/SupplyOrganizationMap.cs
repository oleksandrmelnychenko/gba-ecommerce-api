using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrganizationMap : EntityBaseMap<SupplyOrganization> {
    public override void Map(EntityTypeBuilder<SupplyOrganization> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrganization");

        entity.Property(e => e.Name).HasMaxLength(255);

        entity.Property(e => e.Address).HasMaxLength(255);

        entity.Property(e => e.PhoneNumber).HasMaxLength(255);

        entity.Property(e => e.EmailAddress).HasMaxLength(255);

        entity.Property(e => e.Requisites).HasMaxLength(255);

        entity.Property(e => e.Swift).HasMaxLength(255);

        entity.Property(e => e.SwiftBic).HasMaxLength(255);

        entity.Property(e => e.IntermediaryBank).HasMaxLength(255);

        entity.Property(e => e.BeneficiaryBank).HasMaxLength(255);

        entity.Property(e => e.AccountNumber).HasMaxLength(255);

        entity.Property(e => e.Beneficiary).HasMaxLength(255);

        entity.Property(e => e.Bank).HasMaxLength(255);

        entity.Property(e => e.BankAccount).HasMaxLength(255);

        entity.Property(e => e.NIP).HasMaxLength(255);

        entity.Property(e => e.BankAccountPLN).HasMaxLength(255);

        entity.Property(e => e.BankAccountEUR).HasMaxLength(255);

        entity.Property(e => e.ContactPersonName).HasMaxLength(255);

        entity.Property(e => e.ContactPersonPhone).HasMaxLength(255);

        entity.Property(e => e.ContactPersonEmail).HasMaxLength(255);

        entity.Property(e => e.ContactPersonViber).HasMaxLength(255);

        entity.Property(e => e.ContactPersonSkype).HasMaxLength(255);

        entity.Property(e => e.ContactPersonComment).HasMaxLength(255);

        entity.Ignore(e => e.TotalAgreementsCurrentAmount);

        entity.Ignore(e => e.TotalAgreementsCurrentEuroAmount);

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID");

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID");

        entity.Property(e => e.OriginalRegionCode).HasMaxLength(10);

        entity.Property(e => e.SourceAmgId).HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasMaxLength(16);

        entity.Property(e => e.OriginalRegionCode).HasMaxLength(10);
    }
}