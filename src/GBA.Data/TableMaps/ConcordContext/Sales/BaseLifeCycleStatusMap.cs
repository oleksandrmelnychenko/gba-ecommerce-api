using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class BaseLifeCycleStatusMap : EntityBaseMap<BaseLifeCycleStatus> {
    public override void Map(EntityTypeBuilder<BaseLifeCycleStatus> entity) {
        base.Map(entity);

        entity.ToTable("BaseLifeCycleStatus");
    }
}