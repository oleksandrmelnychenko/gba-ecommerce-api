using GBA.Domain.Entities.Synchronizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Synchronizations;

public sealed class DataSyncOperationMap : EntityBaseMap<DataSyncOperation> {
    public override void Map(EntityTypeBuilder<DataSyncOperation> entity) {
        base.Map(entity);

        entity.ToTable("DataSyncOperation");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.DataSyncOperations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}