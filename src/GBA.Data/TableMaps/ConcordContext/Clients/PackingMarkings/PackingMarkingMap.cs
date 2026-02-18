using GBA.Domain.Entities.Clients.PackingMarkings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PackingMarkings;

public sealed class PackingMarkingMap : EntityBaseMap<PackingMarking> {
    public override void Map(EntityTypeBuilder<PackingMarking> entity) {
        base.Map(entity);

        entity.ToTable("PackingMarking");
    }
}