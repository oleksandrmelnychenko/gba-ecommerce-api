using GBA.Domain.Entities.PolishUserDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PolishUserDetails;

public sealed class ResidenceCardMap : EntityBaseMap<ResidenceCard> {
    public override void Map(EntityTypeBuilder<ResidenceCard> entity) {
        base.Map(entity);

        entity.ToTable("ResidenceCard");
    }
}