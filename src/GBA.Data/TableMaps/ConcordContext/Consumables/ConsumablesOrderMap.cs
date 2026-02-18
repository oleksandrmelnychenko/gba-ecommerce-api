using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumablesOrderMap : EntityBaseMap<ConsumablesOrder> {
    public override void Map(EntityTypeBuilder<ConsumablesOrder> entity) {
        base.Map(entity);

        entity.ToTable("ConsumablesOrder");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.OrganizationNumber).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.ConsumablesStorageId).HasColumnName("ConsumablesStorageID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountWithoutVAT);

        entity.Ignore(e => e.ConsumableProductOrganization);

        entity.Ignore(e => e.SupplyOrganizationAgreement);

        entity.HasOne(e => e.User)
            .WithMany(e => e.ConsumablesOrders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumablesStorage)
            .WithMany(e => e.ConsumablesOrders)
            .HasForeignKey(e => e.ConsumablesStorageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithOne(e => e.ConsumablesOrder)
            .HasForeignKey<ConsumablesOrder>(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}