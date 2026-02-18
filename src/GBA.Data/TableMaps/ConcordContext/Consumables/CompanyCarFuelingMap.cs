using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class CompanyCarFuelingMap : EntityBaseMap<CompanyCarFueling> {
    public override void Map(EntityTypeBuilder<CompanyCarFueling> entity) {
        base.Map(entity);

        entity.ToTable("CompanyCarFueling");

        entity.Property(e => e.CompanyCarId).HasColumnName("CompanyCarID");

        entity.Property(e => e.OutcomePaymentOrderId).HasColumnName("OutcomePaymentOrderID");

        entity.Property(e => e.ConsumableProductOrganizationId).HasColumnName("ConsumableProductOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.PricePerLiter).HasColumnType("money");

        entity.Property(e => e.TotalPrice).HasColumnType("money");

        entity.Property(e => e.TotalPriceWithVat).HasColumnType("money");

        entity.Property(e => e.VatAmount).HasColumnType("money");

        entity.HasOne(e => e.CompanyCar)
            .WithMany(e => e.CompanyCarFuelings)
            .HasForeignKey(e => e.CompanyCarId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OutcomePaymentOrder)
            .WithMany(e => e.CompanyCarFuelings)
            .HasForeignKey(e => e.OutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumableProductOrganization)
            .WithMany(e => e.CompanyCarFuelings)
            .HasForeignKey(e => e.ConsumableProductOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.CompanyCarFuelings)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.CompanyCarFuelings)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}