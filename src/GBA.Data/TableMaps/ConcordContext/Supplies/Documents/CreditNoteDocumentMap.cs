using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class CreditNoteDocumentMap : EntityBaseMap<CreditNoteDocument> {
    public override void Map(EntityTypeBuilder<CreditNoteDocument> entity) {
        base.Map(entity);

        entity.ToTable("CreditNoteDocument");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.CreditNoteDocuments)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}