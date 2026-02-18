using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class AgreementTypeCivilCodeMap : EntityBaseMap<AgreementTypeCivilCode> {
    public override void Map(EntityTypeBuilder<AgreementTypeCivilCode> entity) {
        base.Map(entity);

        entity.ToTable("AgreementTypeCivilCode");

        entity.Property(e => e.CodeOneC).HasMaxLength(25);

        entity.Property(e => e.NameUK).HasMaxLength(100);

        entity.Property(e => e.NamePL).HasMaxLength(100);
    }
}