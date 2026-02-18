using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocolMap : EntityBaseMap<DeliveryProductProtocol> {
    public override void Map(EntityTypeBuilder<DeliveryProductProtocol> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryProductProtocol");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.Id).HasColumnName("ID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.DeliveryProductProtocolNumberId).HasColumnName("DeliveryProductProtocolNumberID");

        entity.Property(x => x.TransportationType).HasDefaultValueSql("0");

        entity.Ignore(x => x.QtyInvoices);
        entity.Ignore(x => x.TotalValue);

        entity.Ignore(x => x.Qty);
        entity.Ignore(x => x.NetPrice);
        entity.Ignore(x => x.GrossPrice);
        entity.Ignore(x => x.AccountingGrossPrice);

        entity.HasOne(x => x.User)
            .WithMany(x => x.DeliveryProductProtocols)
            .HasForeignKey(x => x.UserId);

        entity.HasOne(x => x.Organization)
            .WithMany(x => x.DeliveryProductProtocols)
            .HasForeignKey(x => x.OrganizationId);

        entity.HasOne(x => x.DeliveryProductProtocolNumber)
            .WithOne(x => x.DeliveryProductProtocol)
            .HasForeignKey<DeliveryProductProtocol>(x => x.DeliveryProductProtocolNumberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}