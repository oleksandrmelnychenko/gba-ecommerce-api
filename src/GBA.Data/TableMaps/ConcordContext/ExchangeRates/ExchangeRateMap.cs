using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ExchangeRates;

public sealed class ExchangeRateMap : EntityBaseMap<ExchangeRate> {
    public override void Map(EntityTypeBuilder<ExchangeRate> entity) {
        base.Map(entity);

        entity.ToTable("ExchangeRate");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.HasOne(e => e.AssignedCurrency)
            .WithMany(e => e.ExchangeRates)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}