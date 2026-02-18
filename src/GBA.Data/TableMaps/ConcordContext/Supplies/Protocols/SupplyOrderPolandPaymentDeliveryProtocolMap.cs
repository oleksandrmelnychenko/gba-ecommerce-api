using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderPolandPaymentDeliveryProtocolMap : EntityBaseMap<SupplyOrderPolandPaymentDeliveryProtocol> {
    public override void Map(EntityTypeBuilder<SupplyOrderPolandPaymentDeliveryProtocol> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderPolandPaymentDeliveryProtocol");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Vat).HasColumnType("money");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.SupplyOrderPaymentDeliveryProtocolKeyId).HasColumnName("SupplyOrderPaymentDeliveryProtocolKeyID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyOrderPolandPaymentDeliveryProtocols)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyOrderPolandPaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.SupplyOrderPolandPaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}