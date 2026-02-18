using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInformationTaskMap : EntityBaseMap<SupplyInformationTask> {
    public override void Map(EntityTypeBuilder<SupplyInformationTask> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInformationTask");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.DeletedById).HasColumnName("DeletedByID");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyInformationTasks)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedSupplyInformationTasks)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeletedBy)
            .WithMany(e => e.DeletedSupplyInformationTasks)
            .HasForeignKey(e => e.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}