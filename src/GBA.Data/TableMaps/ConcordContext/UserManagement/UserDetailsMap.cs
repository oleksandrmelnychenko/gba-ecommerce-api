using GBA.Domain.Entities.PolishUserDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserDetailsMap : EntityBaseMap<UserDetails> {
    public override void Map(EntityTypeBuilder<UserDetails> entity) {
        base.Map(entity);

        entity.ToTable("UserDetails");

        entity.Property(e => e.ResidenceCardId).HasColumnName("ResidenceCardID");

        entity.Property(e => e.WorkingContractId).HasColumnName("WorkingContractID");

        entity.Property(e => e.WorkPermitId).HasColumnName("WorkPermitID");

        entity.HasOne(e => e.ResidenceCard)
            .WithMany(e => e.UsersDetails)
            .HasForeignKey(e => e.ResidenceCardId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.WorkingContract)
            .WithMany(e => e.UsersDetails)
            .HasForeignKey(e => e.WorkingContractId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.WorkPermit)
            .WithMany(e => e.UsersDetails)
            .HasForeignKey(e => e.WorkPermitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}