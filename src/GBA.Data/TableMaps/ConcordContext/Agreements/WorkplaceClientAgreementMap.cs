using GBA.Domain.Entities.Agreements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class WorkplaceClientAgreementMap : EntityBaseMap<WorkplaceClientAgreement> {
    public override void Map(EntityTypeBuilder<WorkplaceClientAgreement> entity) {
        base.Map(entity);

        entity.ToTable("WorkplaceClientAgreement");

        entity.Property(e => e.WorkplaceId).HasColumnName("WorkplaceID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.HasOne(e => e.Workplace)
            .WithMany(e => e.WorkplaceClientAgreements)
            .HasForeignKey(e => e.WorkplaceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.WorkplaceClientAgreements)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}