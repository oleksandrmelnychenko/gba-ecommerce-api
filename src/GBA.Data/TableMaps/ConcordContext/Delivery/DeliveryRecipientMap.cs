using GBA.Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Delivery;

public sealed class DeliveryRecipientMap : EntityBaseMap<DeliveryRecipient> {
    public override void Map(EntityTypeBuilder<DeliveryRecipient> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryRecipient");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.DeliveryRecipients)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}