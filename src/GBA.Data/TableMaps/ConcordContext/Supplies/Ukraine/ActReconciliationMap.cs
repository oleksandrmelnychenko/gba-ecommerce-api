using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class ActReconciliationMap : EntityBaseMap<ActReconciliation> {
    public override void Map(EntityTypeBuilder<ActReconciliation> entity) {
        base.Map(entity);

        entity.ToTable("ActReconciliation");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ActReconciliations)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.ActReconciliations)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.ActReconciliations)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}