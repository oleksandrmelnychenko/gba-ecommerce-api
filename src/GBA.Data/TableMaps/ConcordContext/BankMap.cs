using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class BankMap : EntityBaseMap<Bank> {
    public override void Map(EntityTypeBuilder<Bank> entity) {
        base.Map(entity);

        entity.ToTable("Bank");

        entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

        entity.Property(e => e.MfoCode).HasMaxLength(6).IsRequired();

        entity.Property(e => e.EdrpouCode).HasMaxLength(10).IsRequired();

        entity.Property(e => e.Address).HasMaxLength(150);

        entity.Property(e => e.City).HasMaxLength(150);

        entity.Property(e => e.Phones).HasMaxLength(100);
    }
}