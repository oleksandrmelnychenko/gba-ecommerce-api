using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class CalculationTypeMap : EntityBaseMap<CalculationType> {
    public override void Map(EntityTypeBuilder<CalculationType> entity) {
        base.Map(entity);

        entity.ToTable("CalculationType");

        entity.Property(e => e.Name).HasMaxLength(25);
    }
}