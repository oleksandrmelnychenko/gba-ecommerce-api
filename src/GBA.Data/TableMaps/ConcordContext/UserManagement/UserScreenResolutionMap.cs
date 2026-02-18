using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserScreenResolutionMap : EntityBaseMap<UserScreenResolution> {
    public override void Map(EntityTypeBuilder<UserScreenResolution> entity) {
        base.Map(entity);

        entity.ToTable("UserScreenResolution");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.UserScreenResolutions)
            .HasForeignKey(e => e.UserId);
    }
}