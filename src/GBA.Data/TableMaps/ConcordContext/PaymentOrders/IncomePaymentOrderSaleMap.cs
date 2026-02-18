using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class IncomePaymentOrderSaleMap : EntityBaseMap<IncomePaymentOrderSale> {
    public override void Map(EntityTypeBuilder<IncomePaymentOrderSale> entity) {
        base.Map(entity);

        entity.ToTable("IncomePaymentOrderSale");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.OverpaidAmount).HasColumnType("money");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Property(e => e.IncomePaymentOrderId).HasColumnName("IncomePaymentOrderID");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.ReSaleId).HasColumnName("ReSaleID");

        entity.HasOne(e => e.IncomePaymentOrder)
            .WithMany(e => e.IncomePaymentOrderSales)
            .HasForeignKey(e => e.IncomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.IncomePaymentOrderSales)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ReSale)
            .WithMany(e => e.IncomePaymentOrderSales)
            .HasForeignKey(e => e.ReSaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}