using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class DebtMap : EntityBaseMap<Debt> {
    public override void Map(EntityTypeBuilder<Debt> entity) {
        base.Map(entity);

        entity.ToTable("Debt");

        entity.Property(e => e.Total).HasColumnType("decimal(30,14)");

        entity.Ignore(e => e.EuroTotal);
    }
}