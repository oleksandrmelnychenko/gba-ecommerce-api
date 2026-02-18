using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientBalanceMovementMap : EntityBaseMap<ClientBalanceMovement> {
    public override void Map(EntityTypeBuilder<ClientBalanceMovement> entity) {
        base.Map(entity);

        entity.ToTable("ClientBalanceMovement");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.ExchangeRateAmount).HasColumnType("money");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.ClientBalanceMovements)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}