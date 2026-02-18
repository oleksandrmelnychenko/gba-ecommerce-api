using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class ChartMonthMap : EntityBaseMap<ChartMonth> {
    public override void Map(EntityTypeBuilder<ChartMonth> entity) {
        base.Map(entity);

        entity.ToTable("ChartMonth");
    }
}