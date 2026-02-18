using GBA.Domain.AuditEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Auditing;

public sealed class AuditEntityMap : EntityBaseMap<AuditEntity> {
    public override void Map(EntityTypeBuilder<AuditEntity> entity) {
        base.Map(entity);

        entity.ToTable("AuditEntity");

        entity.Property(e => e.BaseEntityNetUid).HasColumnName("BaseEntityNetUID");

        entity.Property(e => e.UpdatedByNetUid).HasColumnName("UpdatedByNetUID");

        entity.Ignore(e => e.OldValues);

        entity.HasIndex(e => e.BaseEntityNetUid);
    }
}