using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine.Documents;

public sealed class TaxFreeDocumentMap : EntityBaseMap<TaxFreeDocument> {
    public override void Map(EntityTypeBuilder<TaxFreeDocument> entity) {
        base.Map(entity);

        entity.ToTable("TaxFreeDocument");

        entity.Property(e => e.TaxFreeId).HasColumnName("TaxFreeID");

        entity.Property(e => e.DocumentUrl).HasMaxLength(250);

        entity.Property(e => e.FileName).HasMaxLength(250);

        entity.Property(e => e.ContentType).HasMaxLength(250);

        entity.Property(e => e.GeneratedName).HasMaxLength(250);

        entity.HasOne(e => e.TaxFree)
            .WithMany(e => e.TaxFreeDocuments)
            .HasForeignKey(e => e.TaxFreeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}