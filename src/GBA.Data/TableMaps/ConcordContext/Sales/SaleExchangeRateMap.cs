using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleExchangeRateMap : EntityBaseMap<SaleExchangeRate> {
    public override void Map(EntityTypeBuilder<SaleExchangeRate> entity) {
        base.Map(entity);

        entity.ToTable("SaleExchangeRate");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.ExchangeRateId).HasColumnName("ExchangeRateID");

        entity.Property(e => e.Value).HasColumnType("money");

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.SaleExchangeRates)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ExchangeRate)
            .WithMany(e => e.SaleExchangeRates)
            .HasForeignKey(e => e.ExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}