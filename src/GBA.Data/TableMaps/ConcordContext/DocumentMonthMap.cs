using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class DocumentMonthMap : EntityBaseMap<DocumentMonth> {
    public override void Map(EntityTypeBuilder<DocumentMonth> entity) {
        base.Map(entity);

        entity.ToTable("DocumentMonth");

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.Name).HasMaxLength(25);
    }
}