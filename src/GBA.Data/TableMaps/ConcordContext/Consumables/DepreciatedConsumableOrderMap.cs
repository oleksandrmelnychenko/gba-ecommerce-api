using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class DepreciatedConsumableOrderMap : EntityBaseMap<DepreciatedConsumableOrder> {
    public override void Map(EntityTypeBuilder<DepreciatedConsumableOrder> entity) {
        base.Map(entity);

        entity.ToTable("DepreciatedConsumableOrder");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(250);

        entity.Property(e => e.CommissionHeadId).HasColumnName("CommissionHeadID");

        entity.Property(e => e.DepreciatedToId).HasColumnName("DepreciatedToID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.ConsumablesStorageId).HasColumnName("ConsumablesStorageID");

        entity.Ignore(e => e.PriceTotals);

        entity.HasOne(e => e.CommissionHead)
            .WithMany(e => e.HeadOfDepreciatedConsumableOrders)
            .HasForeignKey(e => e.CommissionHeadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedTo)
            .WithMany(e => e.DepreciatedConsumableOrders)
            .HasForeignKey(e => e.DepreciatedToId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedDepreciatedConsumableOrders)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedDepreciatedConsumableOrders)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumablesStorage)
            .WithMany(e => e.DepreciatedConsumableOrders)
            .HasForeignKey(e => e.ConsumablesStorageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}