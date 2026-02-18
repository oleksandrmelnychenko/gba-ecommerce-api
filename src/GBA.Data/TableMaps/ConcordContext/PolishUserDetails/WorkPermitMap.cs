using GBA.Domain.Entities.PolishUserDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PolishUserDetails;

public sealed class WorkPermitMap : EntityBaseMap<WorkPermit> {
    public override void Map(EntityTypeBuilder<WorkPermit> entity) {
        base.Map(entity);

        entity.ToTable("WorkPermit");
    }
}