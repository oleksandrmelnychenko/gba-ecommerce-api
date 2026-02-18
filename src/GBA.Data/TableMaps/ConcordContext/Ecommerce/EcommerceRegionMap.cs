using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class EcommerceRegionMap : EntityBaseMap<EcommerceRegion> {
    public override void Map(EntityTypeBuilder<EcommerceRegion> entity) {
        base.Map(entity);

        entity.ToTable("EcommerceRegion");

        entity.Property(e => e.NameUa).HasMaxLength(150);
        entity.Property(e => e.NameRu).HasMaxLength(150);
    }
}