using GBA.Domain.Entities.SaleReturns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.SaleReturns;

public sealed class SaleReturnItemStatusNameMap : EntityBaseMap<SaleReturnItemStatusName> {
    public override void Map(EntityTypeBuilder<SaleReturnItemStatusName> entity) {
        base.Map(entity);

        entity.ToTable("SaleReturnItemStatusName");

        entity.Property(e => e.NameUK).HasMaxLength(120);

        entity.Property(e => e.NamePL).HasMaxLength(120);
    }
}