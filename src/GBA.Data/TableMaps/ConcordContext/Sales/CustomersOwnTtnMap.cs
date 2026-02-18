using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class CustomersOwnTtnMap : EntityBaseMap<CustomersOwnTtn> {
    public override void Map(EntityTypeBuilder<CustomersOwnTtn> entity) {
        base.Map(entity);

        entity.ToTable("CustomersOwnTtn");

        entity.Property(e => e.Number).HasMaxLength(150);
    }
}