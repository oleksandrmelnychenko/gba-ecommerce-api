using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumablesStorageMap : EntityBaseMap<ConsumablesStorage> {
    public override void Map(EntityTypeBuilder<ConsumablesStorage> entity) {
        base.Map(entity);

        entity.ToTable("ConsumablesStorage");

        entity.Property(e => e.Name).HasMaxLength(50);

        entity.Property(e => e.Description).HasMaxLength(250);

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ResponsibleUserId).HasColumnName("ResponsibleUserID");

        entity.Ignore(e => e.ConsumableProducts);

        entity.Ignore(e => e.PriceTotals);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.ConsumablesStorages)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ResponsibleUser)
            .WithMany(e => e.ConsumablesStorages)
            .HasForeignKey(e => e.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}