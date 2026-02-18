using GBA.Domain.Entities.UserNotifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserNotifications;

public sealed class ExpiredBillUserNotificationMap : EntityBaseMap<ExpiredBillUserNotification> {
    public override void Map(EntityTypeBuilder<ExpiredBillUserNotification> entity) {
        base.Map(entity);

        entity.ToTable("ExpiredBillUserNotification");

        entity.Property(e => e.SaleNumber).HasMaxLength(50);

        entity.Property(e => e.FromClient).HasMaxLength(250);

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.ManagerId).HasColumnName("ManagerID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.LockedById).HasColumnName("LockedByID");

        entity.Property(e => e.LastViewedById).HasColumnName("LastViewedByID");

        entity.Property(e => e.ProcessedById).HasColumnName("ProcessedByID");

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.ExpiredBillUserNotifications)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Manager)
            .WithMany(e => e.ManagerExpiredBillUserNotifications)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedByExpiredBillUserNotifications)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.LockedBy)
            .WithMany(e => e.LockedByExpiredBillUserNotifications)
            .HasForeignKey(e => e.LockedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.LastViewedBy)
            .WithMany(e => e.LastViewedByExpiredBillUserNotifications)
            .HasForeignKey(e => e.LastViewedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProcessedBy)
            .WithMany(e => e.ProcessedByExpiredBillUserNotifications)
            .HasForeignKey(e => e.ProcessedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}