using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class ActProvidingServiceDocumentMap : EntityBaseMap<ActProvidingServiceDocument> {
    public override void Map(EntityTypeBuilder<ActProvidingServiceDocument> entity) {
        base.Map(entity);

        entity.ToTable("ActProvidingServiceDocument");

        entity.Property(e => e.Number).HasMaxLength(20);

        entity.Property(e => e.DocumentUrl).HasMaxLength(500);

        entity.Property(e => e.FileName).HasMaxLength(500);

        entity.Property(e => e.ContentType).HasMaxLength(500);

        entity.Property(e => e.GeneratedName).HasMaxLength(500);
    }
}