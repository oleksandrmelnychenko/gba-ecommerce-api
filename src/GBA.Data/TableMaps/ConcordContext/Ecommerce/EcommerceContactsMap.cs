using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class EcommerceContactsMap : EntityBaseMap<EcommerceContacts> {
    public override void Map(EntityTypeBuilder<EcommerceContacts> entity) {
        base.Map(entity);

        entity.ToTable("EcommerceContacts");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Phone).HasMaxLength(30);

        entity.Property(e => e.Skype).HasMaxLength(150);

        entity.Property(e => e.Icq).HasMaxLength(20);

        entity.Property(e => e.Email).HasMaxLength(150);

        entity.Property(e => e.ImgUrl).HasMaxLength(4000);
    }
}