using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class SeoPageMap : EntityBaseMap<SeoPage> {
    public override void Map(EntityTypeBuilder<SeoPage> entity) {
        base.Map(entity);

        entity.ToTable("SeoPage");

        entity.Property(e => e.Title).HasMaxLength(100);

        entity.Property(e => e.Description).HasMaxLength(1000);

        entity.Property(e => e.KeyWords).HasMaxLength(1000);

        entity.Property(e => e.Url).HasMaxLength(1000);

        entity.Property(e => e.LdJson).HasMaxLength(4000);
    }
}