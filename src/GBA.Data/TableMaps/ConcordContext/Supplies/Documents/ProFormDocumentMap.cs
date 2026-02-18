using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class ProFormDocumentMap : EntityBaseMap<ProFormDocument> {
    public override void Map(EntityTypeBuilder<ProFormDocument> entity) {
        base.Map(entity);

        entity.ToTable("ProFormDocument");

        entity.Property(e => e.SupplyProFormId).HasColumnName("SupplyProFormID");

        entity.HasOne(e => e.SupplyProForm)
            .WithMany(e => e.ProFormDocuments)
            .HasForeignKey(e => e.SupplyProFormId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}