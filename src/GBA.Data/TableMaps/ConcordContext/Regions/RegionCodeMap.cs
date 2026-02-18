using GBA.Domain.Entities.Regions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Regions;

public sealed class RegionCodeMap : EntityBaseMap<RegionCode> {
    public override void Map(EntityTypeBuilder<RegionCode> entity) {
        base.Map(entity);

        entity.ToTable("RegionCode");

        entity.Property(e => e.RegionId).HasColumnName("RegionID");

        entity.Property(e => e.Value).HasMaxLength(10);

        entity.Property(e => e.City).HasMaxLength(150);

        entity.Property(e => e.District).HasMaxLength(150);

        entity.HasOne(e => e.Region)
            .WithMany(e => e.RegionCodes)
            .HasForeignKey(e => e.RegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}