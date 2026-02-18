using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class TaxInspectionMap : EntityBaseMap<TaxInspection> {
    public override void Map(EntityTypeBuilder<TaxInspection> entity) {
        base.Map(entity);

        entity.ToTable("TaxInspection");

        entity.Property(e => e.InspectionNumber).HasMaxLength(50);

        entity.Property(e => e.InspectionType).HasMaxLength(150);

        entity.Property(e => e.InspectionName).HasMaxLength(250);

        entity.Property(e => e.InspectionRegionName).HasMaxLength(250);

        entity.Property(e => e.InspectionRegionCode).HasMaxLength(50);

        entity.Property(e => e.InspectionAddress).HasMaxLength(250);

        entity.Property(e => e.InspectionUSREOU).HasMaxLength(50);
    }
}