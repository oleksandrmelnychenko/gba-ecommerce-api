using GBA.Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Delivery;

public sealed class DeliveryRecipientAddressMap : EntityBaseMap<DeliveryRecipientAddress> {
    public override void Map(EntityTypeBuilder<DeliveryRecipientAddress> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryRecipientAddress");

        entity.Property(e => e.DeliveryRecipientId).HasColumnName("DeliveryRecipientID");

        entity.Property(e => e.Value).HasMaxLength(500);

        entity.Property(e => e.Department).HasMaxLength(250);

        entity.Property(e => e.City).HasMaxLength(250);

        entity.HasOne(e => e.DeliveryRecipient)
            .WithMany(e => e.DeliveryRecipientAddresses)
            .HasForeignKey(e => e.DeliveryRecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}