using GBA.Domain.Entities.Clients.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientContractDocumentMap : EntityBaseMap<ClientContractDocument> {
    public override void Map(EntityTypeBuilder<ClientContractDocument> entity) {
        base.Map(entity);

        entity.ToTable("ClientContractDocument");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientContractDocuments)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}