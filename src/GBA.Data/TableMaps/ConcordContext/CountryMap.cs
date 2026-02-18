using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class CountryMap : EntityBaseMap<Country> {
    public override void Map(EntityTypeBuilder<Country> entity) {
        base.Map(entity);

        entity.ToTable("Country");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Code).HasMaxLength(25);
    }
}