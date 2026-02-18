using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class EcommercePageMap : EntityBaseMap<EcommercePage> {
    public override void Map(EntityTypeBuilder<EcommercePage> entity) {
        base.Map(entity);

        entity.ToTable("EcommercePage");

        entity.Property(e => e.TitleUa).HasMaxLength(100);

        entity.Property(e => e.TitleRu).HasMaxLength(100);

        entity.Property(e => e.DescriptionUa).HasMaxLength(1000);

        entity.Property(e => e.DescriptionRu).HasMaxLength(1000);

        entity.Property(e => e.KeyWords).HasMaxLength(1000);

        entity.Property(e => e.UrlUa).HasMaxLength(1000);

        entity.Property(e => e.LdJson).HasMaxLength(4000);

        entity.Property(e => e.UrlRu).HasMaxLength(1000);
    }
}