using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderPaymentDeliveryProtocolMap : EntityBaseMap<SupplyOrderPaymentDeliveryProtocol> {
    public override void Map(EntityTypeBuilder<SupplyOrderPaymentDeliveryProtocol> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderPaymentDeliveryProtocol");

        entity.Property(e => e.Value).HasColumnType("money");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.SupplyProFormId).HasColumnName("SupplyProFormID");

        entity.Property(e => e.SupplyOrderPaymentDeliveryProtocolKeyId).HasColumnName("SupplyOrderPaymentDeliveryProtocolKeyID");

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.PaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.PaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyProForm)
            .WithMany(e => e.PaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyProFormId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyOrderPaymentDeliveryProtocols)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}