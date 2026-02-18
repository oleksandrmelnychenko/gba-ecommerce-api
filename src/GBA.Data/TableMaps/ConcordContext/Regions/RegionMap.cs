using GBA.Domain.Entities.Regions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Regions;

public sealed class RegionMap : EntityBaseMap<Region> {
    public override void Map(EntityTypeBuilder<Region> entity) {
        base.Map(entity);

        entity.ToTable("Region");

        entity.Property(e => e.Name).HasMaxLength(5);
    }
}