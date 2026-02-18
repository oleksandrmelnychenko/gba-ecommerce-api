using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocolMap : EntityBaseMap<SupplyOrderUkrainePaymentDeliveryProtocol> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkrainePaymentDeliveryProtocol> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkrainePaymentDeliveryProtocol");

        entity.Property(e => e.Value).HasColumnType("money");

        entity.Property(e => e.SupplyOrderUkrainePaymentDeliveryProtocolKeyId).HasColumnName("SupplyOrderUkrainePaymentDeliveryProtocolKeyID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.HasOne(e => e.SupplyOrderUkrainePaymentDeliveryProtocolKey)
            .WithMany(e => e.SupplyOrderUkrainePaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyOrderUkrainePaymentDeliveryProtocolKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyOrderUkrainePaymentDeliveryProtocols)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.SupplyOrderUkrainePaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.SupplyOrderUkrainePaymentDeliveryProtocols)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}