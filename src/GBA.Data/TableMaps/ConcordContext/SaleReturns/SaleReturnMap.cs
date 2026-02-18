using GBA.Domain.Entities.SaleReturns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.SaleReturns;

public sealed class SaleReturnMap : EntityBaseMap<SaleReturn> {
    public override void Map(EntityTypeBuilder<SaleReturn> entity) {
        base.Map(entity);

        entity.ToTable("SaleReturn");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.CanceledById).HasColumnName("CanceledByID");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.IsCanceled).HasDefaultValueSql("0");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.Storage);

        entity.Ignore(e => e.Sale);

        entity.Ignore(e => e.Currency);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalCount);

        entity.Ignore(e => e.TotalVatAmountLocal);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.ExchangeRate);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.SaleReturns)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.SaleReturns)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedSaleReturns)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CanceledBy)
            .WithMany(e => e.CanceledSaleReturns)
            .HasForeignKey(e => e.CanceledById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedSaleReturns)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}