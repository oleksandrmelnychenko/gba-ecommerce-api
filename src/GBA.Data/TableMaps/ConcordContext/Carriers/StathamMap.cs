using GBA.Domain.Entities.Carriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Carriers;

public sealed class StathamMap : EntityBaseMap<Statham> {
    public override void Map(EntityTypeBuilder<Statham> entity) {
        base.Map(entity);

        entity.ToTable("Statham");

        entity.Property(e => e.FirstName).HasMaxLength(50);

        entity.Property(e => e.LastName).HasMaxLength(50);

        entity.Property(e => e.MiddleName).HasMaxLength(50);
    }
}