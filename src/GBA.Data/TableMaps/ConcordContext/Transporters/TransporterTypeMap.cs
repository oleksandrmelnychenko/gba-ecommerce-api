using GBA.Domain.Entities.Transporters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Transporters;

public sealed class TransporterTypeMap : EntityBaseMap<TransporterType> {
    public override void Map(EntityTypeBuilder<TransporterType> entity) {
        base.Map(entity);

        entity.ToTable("TransporterType");

        entity.Property(e => e.Name).HasMaxLength(50);
    }
}