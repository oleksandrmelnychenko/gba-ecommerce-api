using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class RetailPaymentMap : EntityBaseMap<RetailPaymentTypeTranslate> {
    public override void Map(EntityTypeBuilder<RetailPaymentTypeTranslate> entity) {
        base.Map(entity);

        entity.ToTable("RetailPaymentTypeTranslate");

        entity.Property(e => e.FullPrice).HasMaxLength(250);
        entity.Property(e => e.LowPrice).HasMaxLength(250);
        entity.Property(e => e.CultureCode).HasMaxLength(5);
        entity.Property(e => e.Comment).HasMaxLength(500);
        entity.Property(e => e.FastOrderSuccessMessage).HasMaxLength(500);
    }
}