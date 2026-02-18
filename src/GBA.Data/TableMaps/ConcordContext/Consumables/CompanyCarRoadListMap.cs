using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class CompanyCarRoadListMap : EntityBaseMap<CompanyCarRoadList> {
    public override void Map(EntityTypeBuilder<CompanyCarRoadList> entity) {
        base.Map(entity);

        entity.ToTable("CompanyCarRoadList");

        entity.Property(e => e.Comment).HasMaxLength(150);

        entity.Property(e => e.CompanyCarId).HasColumnName("CompanyCarID");

        entity.Property(e => e.OutcomePaymentOrderId).HasColumnName("OutcomePaymentOrderID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.HasOne(e => e.CompanyCar)
            .WithMany(e => e.CompanyCarRoadLists)
            .HasForeignKey(e => e.CompanyCarId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OutcomePaymentOrder)
            .WithMany(e => e.CompanyCarRoadLists)
            .HasForeignKey(e => e.OutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.CompanyCarRoadLists)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedCompanyCarRoadLists)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedCompanyCarRoadLists)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}