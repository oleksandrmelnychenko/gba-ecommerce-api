using GBA.Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Delivery;

public sealed class TermsOfDeliveryMap : EntityBaseMap<TermsOfDelivery> {
    public override void Map(EntityTypeBuilder<TermsOfDelivery> entity) {
        base.Map(entity);

        entity.ToTable("TermsOfDelivery");
    }
}