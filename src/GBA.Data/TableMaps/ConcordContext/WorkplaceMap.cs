using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class WorkplaceMap : EntityBaseMap<Workplace> {
    public override void Map(EntityTypeBuilder<Workplace> entity) {
        base.Map(entity);

        entity.ToTable("Workplace");

        entity.Property(e => e.ClientGroupId).HasColumnName("ClientGroupID");

        entity.Property(e => e.MainClientId).HasColumnName("MainClientID");

        entity.Property(e => e.Email).HasMaxLength(150);

        entity.Property(e => e.FirstName).HasMaxLength(150);

        entity.Property(e => e.MiddleName).HasMaxLength(150);

        entity.Property(e => e.LastName).HasMaxLength(150);

        entity.Property(e => e.PhoneNumber).HasMaxLength(16);

        entity.Ignore(e => e.Password);

        entity.HasOne(e => e.MainClient)
            .WithMany(e => e.Workplaces)
            .HasForeignKey(e => e.MainClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientGroup)
            .WithMany(e => e.Workplaces)
            .HasForeignKey(e => e.ClientGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}