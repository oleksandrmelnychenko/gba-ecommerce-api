using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class TaxAccountingSchemeMap : EntityBaseMap<TaxAccountingScheme> {
    public override void Map(EntityTypeBuilder<TaxAccountingScheme> entity) {
        base.Map(entity);

        entity.ToTable("TaxAccountingScheme");

        entity.Property(e => e.CodeOneC).HasMaxLength(25);

        entity.Property(e => e.NameUK).HasMaxLength(100);

        entity.Property(e => e.NamePL).HasMaxLength(100);
    }
}