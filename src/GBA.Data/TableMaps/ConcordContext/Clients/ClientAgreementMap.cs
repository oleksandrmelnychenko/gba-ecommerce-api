using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientAgreementMap : EntityBaseMap<ClientAgreement> {
    public override void Map(EntityTypeBuilder<ClientAgreement> entity) {
        base.Map(entity);

        entity.ToTable("ClientAgreement");

        entity.Property(e => e.AgreementId).HasColumnName("AgreementID");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.CurrentAmount).HasColumnType("money");

        entity.HasOne(e => e.Agreement)
            .WithMany(e => e.ClientAgreements)
            .HasForeignKey(e => e.AgreementId);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientAgreements)
            .HasForeignKey(e => e.ClientId);

        entity.HasIndex(e => e.NetUid);

        entity.Ignore(e => e.AccountBalance);

        entity.Ignore(e => e.FromAmg);

        entity.Ignore(e => e.OriginalClientName);
    }
}