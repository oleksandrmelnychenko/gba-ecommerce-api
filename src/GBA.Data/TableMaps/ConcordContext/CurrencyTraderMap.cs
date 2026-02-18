using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class CurrencyTraderMap : EntityBaseMap<CurrencyTrader> {
    public override void Map(EntityTypeBuilder<CurrencyTrader> entity) {
        base.Map(entity);

        entity.ToTable("CurrencyTrader");

        entity.Property(e => e.FirstName).HasMaxLength(75);

        entity.Property(e => e.LastName).HasMaxLength(75);

        entity.Property(e => e.MiddleName).HasMaxLength(75);

        entity.Property(e => e.PhoneNumber).HasMaxLength(30);
    }
}