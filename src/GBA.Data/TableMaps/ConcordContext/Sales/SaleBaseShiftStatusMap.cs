using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleBaseShiftStatusMap : EntityBaseMap<SaleBaseShiftStatus> {
    public override void Map(EntityTypeBuilder<SaleBaseShiftStatus> entity) {
        base.Map(entity);

        entity.ToTable("SaleBaseShiftStatus");
    }
}