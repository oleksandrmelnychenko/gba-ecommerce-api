using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumableProductCategoryMap : EntityBaseMap<ConsumableProductCategory> {
    public override void Map(EntityTypeBuilder<ConsumableProductCategory> entity) {
        base.Map(entity);

        entity.ToTable("ConsumableProductCategory");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Description).HasMaxLength(450);
    }
}