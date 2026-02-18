using GBA.Domain.Entities.Products.Incomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products.Incomes;

public sealed class ProductIncomeMap : EntityBaseMap<ProductIncome> {
    public override void Map(EntityTypeBuilder<ProductIncome> entity) {
        base.Map(entity);

        entity.ToTable("ProductIncome");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Ignore(e => e.Organization);

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.AccountingTotalNetPrice);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalQty);

        entity.Ignore(e => e.Currency);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalGrossPrice);

        entity.Ignore(e => e.ExchangeRateToUah);

        entity.Ignore(e => e.PackingList);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.TotalNetWithVat);

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.ProductIncomes)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.ProductIncomes)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}