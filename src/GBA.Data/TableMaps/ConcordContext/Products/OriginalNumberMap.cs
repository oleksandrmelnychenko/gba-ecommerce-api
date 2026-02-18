using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Organizations;

public sealed class OriginalNumberMap : EntityBaseMap<OriginalNumber> {
    public override void Map(EntityTypeBuilder<OriginalNumber> entity) {
        base.Map(entity);

        entity.ToTable("OriginalNumber");
    }
}