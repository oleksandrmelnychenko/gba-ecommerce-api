using GBA.Domain.Entities.ReSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ReSales;

public sealed class ReSaleMap : EntityBaseMap<ReSale> {
    public override void Map(EntityTypeBuilder<ReSale> entity) {
        base.Map(entity);

        entity.ToTable("ReSale");

        entity.Property(e => e.Comment).HasMaxLength(250);

        entity.Property(e => e.SaleNumberId).HasColumnName("SaleNumberID");

        entity.Property(e => e.FromStorageId).HasColumnName("FromStorageID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.BaseLifeCycleStatusId).HasColumnName("BaseLifeCycleStatusID");

        entity.Property(e => e.BaseSalePaymentStatusId).HasColumnName("BaseSalePaymentStatusID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.ChangedToInvoiceById).HasColumnName("ChangedToInvoiceByID");

        entity.Property(e => e.TotalPaymentAmount).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ChangedToInvoiceBy)
            .WithMany(e => e.ChangeToInvoiceReSales)
            .HasForeignKey(e => e.ChangedToInvoiceById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BaseLifeCycleStatus)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.BaseLifeCycleStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BaseSalePaymentStatus)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.BaseSalePaymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.FromStorage)
            .WithMany(e => e.ReSales)
            .HasForeignKey(e => e.FromStorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(x => x.TotalQty);

        entity.Ignore(x => x.TotalPrice);

        entity.Ignore(x => x.TotalAmount);

        entity.Ignore(x => x.TotalAmountLocal);

        entity.Ignore(x => x.TotalAmountEurToUah);

        entity.Ignore(x => x.TotalVat);

        entity.Ignore(x => x.UserFullName);

        entity.Ignore(x => x.DifferencePaymentAndInvoiceAmount);
    }
}