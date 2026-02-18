using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientBankDetailsMap : EntityBaseMap<ClientBankDetails> {
    public override void Map(EntityTypeBuilder<ClientBankDetails> entity) {
        base.Map(entity);

        entity.ToTable("ClientBankDetails");

        entity.Property(e => e.ClientBankDetailIbanNoId).HasColumnName("ClientBankDetailIbanNoID");

        entity.Property(e => e.AccountNumberId).HasColumnName("AccountNumberID");
    }
}