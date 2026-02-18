using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Auditing;

public sealed class AuditEntityPropertyNameTranslationMap : EntityBaseMap<AuditEntityPropertyNameTranslation> {
    public override void Map(EntityTypeBuilder<AuditEntityPropertyNameTranslation> entity) {
        base.Map(entity);

        entity.ToTable("AuditEntityPropertyNameTranslation");
    }
}