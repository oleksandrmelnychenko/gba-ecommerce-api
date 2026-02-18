using GBA.Domain.Entities.Carriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Carriers;

public sealed class StathamPassportMap : EntityBaseMap<StathamPassport> {
    public override void Map(EntityTypeBuilder<StathamPassport> entity) {
        base.Map(entity);

        entity.ToTable("StathamPassport");

        entity.Property(e => e.StathamId).HasColumnName("StathamID");

        entity.Property(e => e.PassportSeria).HasMaxLength(20);

        entity.Property(e => e.PassportNumber).HasMaxLength(20);

        entity.Property(e => e.PassportIssuedBy).HasMaxLength(250);

        entity.Property(e => e.City).HasMaxLength(150);

        entity.Property(e => e.Street).HasMaxLength(150);

        entity.Property(e => e.HouseNumber).HasMaxLength(50);

        entity.HasOne(e => e.Statham)
            .WithMany(e => e.StathamPassports)
            .HasForeignKey(e => e.StathamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}