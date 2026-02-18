using GBA.Domain.Entities.Supplies.ActProvidingServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.ActProvidingServices;

public sealed class ActProvidingServiceMap : EntityBaseMap<ActProvidingService> {
    public override void Map(EntityTypeBuilder<ActProvidingService> entity) {
        base.Map(entity);

        entity.ToTable("ActProvidingService");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.Price).HasColumnType("decimal(30,14)");

        entity.Property(e => e.Comment).HasMaxLength(2000);

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.HasOne(e => e.User)
            .WithMany(x => x.ActProvidingServices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}