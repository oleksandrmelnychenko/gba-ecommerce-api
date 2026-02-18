using GBA.Domain.Entities.AccountingDocumentNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.AccountingDocumentNames;

public sealed class AccountingOperationNameMap : EntityBaseMap<AccountingOperationName> {
    public override void Map(EntityTypeBuilder<AccountingOperationName> entity) {
        base.Map(entity);

        entity.ToTable("AccountingOperationName");

        entity.Property(e => e.BankNameUK).HasMaxLength(120);

        entity.Property(e => e.BankNamePL).HasMaxLength(120);

        entity.Property(e => e.CashNameUK).HasMaxLength(120);

        entity.Property(e => e.CashNamePL).HasMaxLength(120);
    }
}