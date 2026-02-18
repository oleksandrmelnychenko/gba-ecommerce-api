using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class SupplyPaymentTaskDocumentMap : EntityBaseMap<SupplyPaymentTaskDocument> {
    public override void Map(EntityTypeBuilder<SupplyPaymentTaskDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyPaymentTaskDocument");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.SupplyPaymentTaskDocuments)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}