using GBA.Domain.Entities.VatRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.VatRates;

public sealed class VatRateMap : EntityBaseMap<VatRate> {
    public override void Map(EntityTypeBuilder<VatRate> entity) {
        base.Map(entity);

        entity.ToTable("VatRate");
    }
}