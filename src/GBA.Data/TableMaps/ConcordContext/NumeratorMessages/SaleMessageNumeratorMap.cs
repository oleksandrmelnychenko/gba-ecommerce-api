using GBA.Domain.Entities.NumeratorMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.NumeratorMessages;

public sealed class SaleMessageNumeratorMap : EntityBaseMap<SaleMessageNumerator> {
    public override void Map(EntityTypeBuilder<SaleMessageNumerator> entity) {
        base.Map(entity);

        entity.ToTable("SaleMessageNumerator");
    }
}