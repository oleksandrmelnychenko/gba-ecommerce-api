using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ExchangeRates;

public sealed class GovExchangeRateMap : EntityBaseMap<GovExchangeRate> {
    public override void Map(EntityTypeBuilder<GovExchangeRate> entity) {
        base.Map(entity);

        entity.ToTable("GovExchangeRate");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.HasOne(e => e.AssignedCurrency)
            .WithMany(e => e.GovExchangeRates)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}