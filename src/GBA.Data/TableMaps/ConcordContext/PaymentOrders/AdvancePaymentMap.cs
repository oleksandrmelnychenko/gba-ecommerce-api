using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class AdvancePaymentMap : EntityBaseMap<AdvancePayment> {
    public override void Map(EntityTypeBuilder<AdvancePayment> entity) {
        base.Map(entity);

        entity.ToTable("AdvancePayment");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.VatAmount).HasColumnType("money");

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.TaxFreeId).HasColumnName("TaxFreeID");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.OrganizationClientAgreementId).HasColumnName("OrganizationClientAgreementID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFree)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.TaxFreeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClientAgreement)
            .WithMany(e => e.AdvancePayments)
            .HasForeignKey(e => e.OrganizationClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}