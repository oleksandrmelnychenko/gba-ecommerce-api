using GBA.Domain.Entities.Consignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consignments;

public sealed class ConsignmentItemMovementTypeNameMap : EntityBaseMap<ConsignmentItemMovementTypeName> {
    public override void Map(EntityTypeBuilder<ConsignmentItemMovementTypeName> entity) {
        base.Map(entity);

        entity.ToTable("ConsignmentItemMovementTypeName");

        entity.Property(e => e.NameUa).HasMaxLength(100);

        entity.Property(e => e.NamePl).HasMaxLength(100);
    }
}