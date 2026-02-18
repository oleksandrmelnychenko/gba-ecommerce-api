using GBA.Domain.Entities.Pricings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class PriceTypeMap : EntityBaseMap<PriceType> {
    public override void Map(EntityTypeBuilder<PriceType> entity) {
        base.Map(entity);

        entity.ToTable("PriceType");

        entity.Property(e => e.Name).HasMaxLength(30);
    }
}