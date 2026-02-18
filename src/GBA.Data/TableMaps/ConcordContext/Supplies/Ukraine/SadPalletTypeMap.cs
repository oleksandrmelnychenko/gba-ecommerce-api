using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SadPalletTypeMap : EntityBaseMap<SadPalletType> {
    public override void Map(EntityTypeBuilder<SadPalletType> entity) {
        base.Map(entity);

        entity.ToTable("SadPalletType");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.Property(e => e.CssClass).HasMaxLength(50);
    }
}