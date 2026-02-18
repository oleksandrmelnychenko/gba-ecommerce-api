using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class AgreementTypeMap : EntityBaseMap<AgreementType> {
    public override void Map(EntityTypeBuilder<AgreementType> entity) {
        base.Map(entity);

        entity.ToTable("AgreementType");

        entity.Property(e => e.Name).HasMaxLength(25);
    }
}