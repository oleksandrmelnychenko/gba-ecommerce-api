using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class ServiceDetailItemKeyMap : EntityBaseMap<ServiceDetailItemKey> {
    public override void Map(EntityTypeBuilder<ServiceDetailItemKey> entity) {
        base.Map(entity);

        entity.ToTable("ServiceDetailItemKey");
    }
}