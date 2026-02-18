using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientBankDetailIbanNoMap : EntityBaseMap<ClientBankDetailIbanNo> {
    public override void Map(EntityTypeBuilder<ClientBankDetailIbanNo> entity) {
        base.Map(entity);

        entity.ToTable("ClientBankDetailIbanNo");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
    }
}