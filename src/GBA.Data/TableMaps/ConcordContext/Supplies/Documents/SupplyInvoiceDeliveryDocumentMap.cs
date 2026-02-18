using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class SupplyInvoiceDeliveryDocumentMap : EntityBaseMap<SupplyInvoiceDeliveryDocument> {
    public override void Map(EntityTypeBuilder<SupplyInvoiceDeliveryDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInvoiceDeliveryDocument");

        entity.Property(e => e.Number).HasMaxLength(20);
        entity.Property(e => e.DocumentUrl).HasMaxLength(500);
        entity.Property(e => e.ContentType).HasMaxLength(500);
        entity.Property(e => e.FileName).HasMaxLength(500);
        entity.Property(e => e.GeneratedName).HasMaxLength(500);

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");
        entity.Property(e => e.SupplyDeliveryDocumentId).HasColumnName("SupplyDeliveryDocumentID");

        entity.HasOne(x => x.SupplyInvoice)
            .WithMany(x => x.SupplyInvoiceDeliveryDocuments)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SupplyDeliveryDocument)
            .WithMany(x => x.SupplyInvoiceDeliveryDocuments)
            .HasForeignKey(e => e.SupplyDeliveryDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
        ;
    }
}