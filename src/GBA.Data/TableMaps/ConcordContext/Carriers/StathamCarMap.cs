using GBA.Domain.Entities.Carriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Carriers;

public sealed class StathamCarMap : EntityBaseMap<StathamCar> {
    public override void Map(EntityTypeBuilder<StathamCar> entity) {
        base.Map(entity);

        entity.ToTable("StathamCar");

        entity.Property(e => e.StathamId).HasColumnName("StathamID");

        entity.Property(e => e.Number).HasMaxLength(150);

        entity.HasOne(e => e.Statham)
            .WithMany(e => e.StathamCars)
            .HasForeignKey(e => e.StathamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}