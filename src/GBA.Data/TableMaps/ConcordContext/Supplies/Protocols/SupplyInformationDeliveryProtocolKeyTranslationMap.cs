using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInformationDeliveryProtocolKeyTranslationMap : EntityBaseMap<SupplyInformationDeliveryProtocolKeyTranslation> {
    public override void Map(EntityTypeBuilder<SupplyInformationDeliveryProtocolKeyTranslation> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInformationDeliveryProtocolKeyTranslation");

        entity.Property(e => e.SupplyInformationDeliveryProtocolKeyId).HasColumnName("SupplyInformationDeliveryProtocolKeyID");

        entity.HasOne(e => e.SupplyInformationDeliveryProtocolKey)
            .WithMany(e => e.SupplyInformationDeliveryProtocolKeyTranslations)
            .HasForeignKey(e => e.SupplyInformationDeliveryProtocolKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}