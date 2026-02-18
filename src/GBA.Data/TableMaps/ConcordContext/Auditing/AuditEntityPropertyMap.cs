using GBA.Domain.AuditEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Auditing;

public sealed class AuditEntityPropertyMap : EntityBaseMap<AuditEntityProperty> {
    public override void Map(EntityTypeBuilder<AuditEntityProperty> entity) {
        base.Map(entity);

        entity.ToTable("AuditEntityProperty");

        entity.Property(e => e.AuditEntityId).HasColumnName("AuditEntityID");
    }
}