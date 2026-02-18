using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyPaymentTaskMap : EntityBaseMap<SupplyPaymentTask> {
    public override void Map(EntityTypeBuilder<SupplyPaymentTask> entity) {
        base.Map(entity);

        entity.ToTable("SupplyPaymentTask");

        entity.Property(e => e.UserId).HasColumnName("UserID").IsRequired(false);

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.DeletedById).HasColumnName("DeletedByID");

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Ignore(e => e.CurrentTotal);

        entity.Ignore(e => e.EuroNetPrice);

        entity.Ignore(e => e.EuroGrossPrice);

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyPaymentTasks)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedSupplyPaymentTasks)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeletedBy)
            .WithMany(e => e.DeletedSupplyPaymentTasks)
            .HasForeignKey(e => e.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}