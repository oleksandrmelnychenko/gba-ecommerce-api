using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class OrderProductSpecificationMap : EntityBaseMap<OrderProductSpecification> {
    public override void Map(EntityTypeBuilder<OrderProductSpecification> entity) {
        base.Map(entity);

        entity.ToTable("OrderProductSpecification");

        entity.HasOne(e => e.ProductSpecification)
            .WithMany(e => e.OrderProductSpecifications)
            .HasForeignKey(e => e.ProductSpecificationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.OrderProductSpecifications)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.OrderProductSpecifications)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}