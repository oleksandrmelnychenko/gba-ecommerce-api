using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class SupplyOrganizationDocumentMap : EntityBaseMap<SupplyOrganizationDocument> {
    public override void Map(EntityTypeBuilder<SupplyOrganizationDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrganizationDocument");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.SupplyOrganizationDocuments)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}