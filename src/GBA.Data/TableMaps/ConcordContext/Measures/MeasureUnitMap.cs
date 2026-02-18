using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Measures;

public sealed class MeasureUnitMap : EntityBaseMap<MeasureUnit> {
    public override void Map(EntityTypeBuilder<MeasureUnit> entity) {
        base.Map(entity);

        entity.ToTable("MeasureUnit");

        entity.Property(e => e.Name).HasMaxLength(25);

        entity.Property(e => e.CodeOneC).HasMaxLength(25);

        entity.Ignore(e => e.NameUk);

        entity.Ignore(e => e.NamePl);
    }
}