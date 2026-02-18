using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class DeliveryProductProtocolDocumentMap : EntityBaseMap<DeliveryProductProtocolDocument> {
    public override void Map(EntityTypeBuilder<DeliveryProductProtocolDocument> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryProductProtocolDocument");

        entity.Property(e => e.Number).HasMaxLength(20);

        entity.Property(e => e.DocumentUrl).HasMaxLength(500);
        entity.Property(e => e.FileName).HasMaxLength(500);
        entity.Property(e => e.ContentType).HasMaxLength(500);
        entity.Property(e => e.GeneratedName).HasMaxLength(500);

        entity.Property(e => e.DeliveryProductProtocolId).HasColumnName("DeliveryProductProtocolID");

        entity.HasOne(x => x.DeliveryProductProtocol)
            .WithMany(x => x.DeliveryProductProtocolDocuments)
            .HasForeignKey(e => e.DeliveryProductProtocolId)
            .OnDelete(DeleteBehavior.Restrict);
        ;
    }
}