using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class ResponsibilityDeliveryProtocolMap : EntityBaseMap<ResponsibilityDeliveryProtocol> {
    public override void Map(EntityTypeBuilder<ResponsibilityDeliveryProtocol> entity) {
        base.Map(entity);

        entity.ToTable("ResponsibilityDeliveryProtocol");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.ResponsibilityDeliveryProtocols)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.ResponsibilityDeliveryProtocols)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}