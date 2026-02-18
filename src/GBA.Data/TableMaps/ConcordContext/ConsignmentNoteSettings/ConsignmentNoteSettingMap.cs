using GBA.Domain.Entities.ConsignmentNoteSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSettingMap : EntityBaseMap<ConsignmentNoteSetting> {
    public override void Map(EntityTypeBuilder<ConsignmentNoteSetting> entity) {
        base.Map(entity);
        entity.ToTable("ConsignmentNoteSetting");

        entity.Property(e => e.Name).HasMaxLength(200);
        entity.Property(e => e.BrandAndNumberCar).HasMaxLength(200);
        entity.Property(e => e.TrailerNumber).HasMaxLength(200);
        entity.Property(e => e.Driver).HasMaxLength(200);
        entity.Property(e => e.Carrier).HasMaxLength(200);
        entity.Property(e => e.TypeTransportation).HasMaxLength(200);
        entity.Property(e => e.UnloadingPoint).HasMaxLength(500);
        entity.Property(e => e.LoadingPoint).HasMaxLength(500);
        entity.Property(e => e.Customer).HasMaxLength(200);
        entity.Property(e => e.CarLabel).HasMaxLength(200);
        entity.Property(e => e.TrailerLabel).HasMaxLength(200);

        entity.Ignore(x => x.Number);
    }
}