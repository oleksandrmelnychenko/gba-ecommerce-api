using GBA.Domain.Entities.PolishUserDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PolishUserDetails;

public sealed class WorkingContractMap : EntityBaseMap<WorkingContract> {
    public override void Map(EntityTypeBuilder<WorkingContract> entity) {
        base.Map(entity);

        entity.ToTable("WorkingContract");
    }
}