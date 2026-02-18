using GBA.Domain.Entities.Supplies.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Returns;

public sealed class SupplyReturnMap : EntityBaseMap<SupplyReturn> {
    public override void Map(EntityTypeBuilder<SupplyReturn> entity) {
        base.Map(entity);

        entity.ToTable("SupplyReturn");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalQty);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.SupplyReturns)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.SupplyReturns)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.SupplyReturns)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.SupplyReturns)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Supplier)
            .WithMany(e => e.SupplyReturns)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}