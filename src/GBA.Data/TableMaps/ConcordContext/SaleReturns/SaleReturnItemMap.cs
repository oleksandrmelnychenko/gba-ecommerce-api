using GBA.Domain.Entities.SaleReturns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.SaleReturns;

public sealed class SaleReturnItemMap : EntityBaseMap<SaleReturnItem> {
    public override void Map(EntityTypeBuilder<SaleReturnItem> entity) {
        base.Map(entity);

        entity.ToTable("SaleReturnItem");

        entity.Property(e => e.IsMoneyReturned).HasDefaultValueSql("0");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.SaleReturnId).HasColumnName("SaleReturnID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.MoneyReturnedById).HasColumnName("MoneyReturnedByID");

        entity.Property(e => e.ExchangeRateAmount).HasColumnType("money");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)").HasDefaultValueSql("0");

        entity.Ignore(e => e.AmountLocal);

        entity.Ignore(e => e.VatAmountLocal);

        entity.Ignore(e => e.StatusName);

        entity.Ignore(e => e.VatAmount);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.SaleReturnItems)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.SaleReturnItems)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SaleReturn)
            .WithMany(e => e.SaleReturnItems)
            .HasForeignKey(e => e.SaleReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedSaleReturnItems)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedSaleReturnItems)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MoneyReturnedBy)
            .WithMany(e => e.MoneyReturnedSaleReturnItems)
            .HasForeignKey(e => e.MoneyReturnedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}