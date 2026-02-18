using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Currencies;

public sealed class CurrencyMap : EntityBaseMap<Currency> {
    public override void Map(EntityTypeBuilder<Currency> entity) {
        base.Map(entity);

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Code).HasMaxLength(25);

        entity.Property(e => e.CodeOneC).HasMaxLength(25);

        entity.ToTable("Currency");
    }
}