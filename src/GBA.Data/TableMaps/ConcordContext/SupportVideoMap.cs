using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class SupportVideoMap : EntityBaseMap<SupportVideo> {
    public override void Map(EntityTypeBuilder<SupportVideo> entity) {
        base.Map(entity);

        entity.ToTable("SupportVideo");

        entity.Property(e => e.NameUk).HasMaxLength(150);

        entity.Property(e => e.NamePl).HasMaxLength(150);

        entity.Property(e => e.Url).HasMaxLength(250);

        entity.Property(e => e.DocumentUrl).HasMaxLength(250);
    }
}