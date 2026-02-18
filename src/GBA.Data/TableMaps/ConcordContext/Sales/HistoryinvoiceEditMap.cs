using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class HistoryInvoiceEditMap : EntityBaseMap<HistoryInvoiceEdit> {
    public override void Map(EntityTypeBuilder<HistoryInvoiceEdit> entity) {
        base.Map(entity);

        entity.ToTable("HistoryInvoiceEdit");

        entity.Property(e => e.IsDevelopment).HasDefaultValueSql("0");

        entity.Property(e => e.IsPrinted).HasDefaultValueSql("0");

        entity.Property(e => e.ApproveUpdate).HasDefaultValueSql("0");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.HistoryInvoiceEdit)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}