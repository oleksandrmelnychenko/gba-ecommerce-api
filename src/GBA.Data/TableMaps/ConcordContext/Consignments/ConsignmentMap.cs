using GBA.Domain.Entities.Consignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consignments;

public sealed class ConsignmentMap : EntityBaseMap<Consignment> {
    public override void Map(EntityTypeBuilder<Consignment> entity) {
        base.Map(entity);

        entity.ToTable("Consignment");

        entity.Property(e => e.IsVirtual).HasDefaultValueSql("0");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ProductIncomeId).HasColumnName("ProductIncomeID");

        entity.Property(e => e.ProductTransferId).HasColumnName("ProductTransferID");

        entity.Property(e => e.IsImportedFromOneC).HasDefaultValueSql("0");

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.Consignments)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.Consignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductIncome)
            .WithMany(e => e.Consignments)
            .HasForeignKey(e => e.ProductIncomeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductTransfer)
            .WithMany(e => e.Consignments)
            .HasForeignKey(e => e.ProductTransferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}