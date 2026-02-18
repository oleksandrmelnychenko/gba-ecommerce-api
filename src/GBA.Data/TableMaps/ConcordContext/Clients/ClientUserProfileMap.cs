using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientUserProfileMap : EntityBaseMap<ClientUserProfile> {
    public override void Map(EntityTypeBuilder<ClientUserProfile> entity) {
        base.Map(entity);

        entity.ToTable("ClientUserProfile");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.UserProfileId).HasColumnName("UserProfileID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientManagers)
            .HasForeignKey(e => e.ClientId);

        entity.HasOne(e => e.UserProfile)
            .WithMany(e => e.ClientUserProfiles)
            .HasForeignKey(e => e.UserProfileId);
    }
}