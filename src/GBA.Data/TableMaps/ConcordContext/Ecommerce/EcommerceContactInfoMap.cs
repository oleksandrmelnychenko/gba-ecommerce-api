using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class EcommerceContactInfoMap : EntityBaseMap<EcommerceContactInfo> {
    public override void Map(EntityTypeBuilder<EcommerceContactInfo> entity) {
        base.Map(entity);

        entity.ToTable("EcommerceContactInfo");

        entity.Property(e => e.Address)
            .HasMaxLength(250)
            .IsRequired();

        entity.Property(e => e.Email)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(e => e.Phone)
            .HasMaxLength(30)
            .IsRequired();

        entity.Property(e => e.SiteUrl)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(e => e.Locale)
            .HasMaxLength(2);
        //.IsRequired();

        entity.Property(e => e.PixelId)
            .HasMaxLength(200);
    }
}