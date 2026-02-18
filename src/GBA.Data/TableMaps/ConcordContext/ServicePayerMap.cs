using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class ServicePayerMap : EntityBaseMap<ServicePayer> {
    public override void Map(EntityTypeBuilder<ServicePayer> entity) {
        base.Map(entity);

        entity.ToTable("ServicePayer");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ServicePayers)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}