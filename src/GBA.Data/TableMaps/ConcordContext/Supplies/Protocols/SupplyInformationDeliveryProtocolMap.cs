using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInformationDeliveryProtocolMap : EntityBaseMap<SupplyInformationDeliveryProtocol> {
    public override void Map(EntityTypeBuilder<SupplyInformationDeliveryProtocol> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInformationDeliveryProtocol");

        entity.Property(e => e.IsDefault).HasDefaultValueSql("0");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.SupplyProFormId).HasColumnName("SupplyProFormID");

        entity.Property(e => e.SupplyInformationDeliveryProtocolKeyId).HasColumnName("SupplyInformationDeliveryProtocolKeyID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyInformationDeliveryProtocols)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.InformationDeliveryProtocols)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.InformationDeliveryProtocols)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyProForm)
            .WithMany(e => e.InformationDeliveryProtocols)
            .HasForeignKey(e => e.SupplyProFormId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}