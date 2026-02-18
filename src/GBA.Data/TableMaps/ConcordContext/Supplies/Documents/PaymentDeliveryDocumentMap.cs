using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class PaymentDeliveryDocumentMap : EntityBaseMap<PaymentDeliveryDocument> {
    public override void Map(EntityTypeBuilder<PaymentDeliveryDocument> entity) {
        base.Map(entity);

        entity.ToTable("PaymentDeliveryDocument");

        entity.Property(e => e.SupplyOrderPaymentDeliveryProtocolId).HasColumnName("SupplyOrderPaymentDeliveryProtocolID");

        entity.HasOne(e => e.SupplyOrderPaymentDeliveryProtocol)
            .WithMany(e => e.PaymentDeliveryDocuments)
            .HasForeignKey(e => e.SupplyOrderPaymentDeliveryProtocolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}