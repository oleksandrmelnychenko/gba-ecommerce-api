using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class IncotermMap : EntityBaseMap<Incoterm> {
    public override void Map(EntityTypeBuilder<Incoterm> entity) {
        base.Map(entity);

        entity.ToTable("Incoterm");

        entity.Property(e => e.IncotermName).HasMaxLength(250);
    }
}