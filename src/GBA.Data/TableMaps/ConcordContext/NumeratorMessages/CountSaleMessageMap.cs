using GBA.Domain.Entities.NumeratorMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.NumeratorMessages;

public sealed class CountSaleMessageMap : EntityBaseMap<CountSaleMessage> {
    public override void Map(EntityTypeBuilder<CountSaleMessage> entity) {
        base.Map(entity);

        entity.ToTable("CountSaleMessage");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.SaleMessageNumeratorId).HasColumnName("SaleMessageNumeratorID");

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.CountSaleMessages)
            .HasForeignKey(e => e.SaleId);

        entity.HasOne(e => e.SaleMessageNumerator)
            .WithMany(e => e.CountSaleMessages)
            .HasForeignKey(e => e.SaleMessageNumeratorId);
    }
}