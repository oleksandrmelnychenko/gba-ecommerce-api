using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientBankDetailAccountNumberMap : EntityBaseMap<ClientBankDetailAccountNumber> {
    public override void Map(EntityTypeBuilder<ClientBankDetailAccountNumber> entity) {
        base.Map(entity);

        entity.ToTable("ClientBankDetailAccountNumber");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
    }
}