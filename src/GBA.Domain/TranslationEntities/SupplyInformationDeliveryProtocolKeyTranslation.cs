using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.TranslationEntities;

public class SupplyInformationDeliveryProtocolKeyTranslation : TranslationEntityBase {
    public string Key { get; set; }

    public long SupplyInformationDeliveryProtocolKeyId { get; set; }

    public virtual SupplyInformationDeliveryProtocolKey SupplyInformationDeliveryProtocolKey { get; set; }
}