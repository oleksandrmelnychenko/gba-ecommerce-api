using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientInDebtMap : EntityBaseMap<ClientInDebt> {
    public override void Map(EntityTypeBuilder<ClientInDebt> entity) {
        base.Map(entity);

        entity.ToTable("ClientInDebt");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.AgreementId).HasColumnName("AgreementID");

        entity.Property(e => e.DebtId).HasColumnName("DebtID");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.ReSaleId).HasColumnName("ReSaleID");

        entity.HasOne(e => e.Agreement)
            .WithMany(e => e.ClientInDebts)
            .HasForeignKey(e => e.AgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Debt)
            .WithMany(e => e.ClientInDebts)
            .HasForeignKey(e => e.DebtId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientInDebts)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.ClientInDebts)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ReSale)
            .WithMany(e => e.ClientInDebts)
            .HasForeignKey(e => e.ReSaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}