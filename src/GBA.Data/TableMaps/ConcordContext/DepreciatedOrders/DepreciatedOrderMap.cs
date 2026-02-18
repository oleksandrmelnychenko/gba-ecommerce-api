using GBA.Domain.Entities.DepreciatedOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.DepreciatedOrders;

public sealed class DepreciatedOrderMap : EntityBaseMap<DepreciatedOrder> {
    public override void Map(EntityTypeBuilder<DepreciatedOrder> entity) {
        base.Map(entity);

        entity.ToTable("DepreciatedOrder");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.StorageId).HasColumnName("StorageID");

        entity.Ignore(e => e.ExchangeRate);

        entity.Ignore(e => e.Amount);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.DepreciatedOrders)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleDepreciatedOrders)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Storage)
            .WithMany(e => e.DepreciatedOrders)
            .HasForeignKey(e => e.StorageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}