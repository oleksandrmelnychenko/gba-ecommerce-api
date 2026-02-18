using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class RetailClientPaymentImageMap : EntityBaseMap<RetailClientPaymentImage> {
    public override void Map(EntityTypeBuilder<RetailClientPaymentImage> entity) {
        base.Map(entity);

        entity.ToTable("RetailClientPaymentImage");

        entity.HasOne(e => e.RetailClient)
            .WithMany(e => e.RetailClientPaymentImages)
            .HasForeignKey(e => e.RetailClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.RetailClientPaymentImages)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RetailPaymentStatus)
            .WithMany(e => e.RetailClientPaymentImages)
            .HasForeignKey(e => e.RetailPaymentStatusId);
    }
}