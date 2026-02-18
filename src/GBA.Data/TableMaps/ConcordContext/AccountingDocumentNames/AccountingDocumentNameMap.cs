using GBA.Domain.Entities.AccountingDocumentNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.AccountingDocumentNames;

public sealed class AccountingDocumentNameMap : EntityBaseMap<AccountingDocumentName> {
    public override void Map(EntityTypeBuilder<AccountingDocumentName> entity) {
        base.Map(entity);

        entity.ToTable("AccountingDocumentName");

        entity.Property(e => e.NameUK).HasMaxLength(120);

        entity.Property(e => e.NamePL).HasMaxLength(120);
    }
}