using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyInformationDeliveryProtocolKey : EntityBase {
    public SupplyInformationDeliveryProtocolKey() {
        SupplyInformationDeliveryProtocolKeyTranslations = new HashSet<SupplyInformationDeliveryProtocolKeyTranslation>();
    }

    public string Key { get; set; }

    public bool IsDefault { get; set; }

    public SupplyTransportationType TransportationType { get; set; }

    public KeyAssignedTo KeyAssignedTo { get; set; }

    public ICollection<SupplyInformationDeliveryProtocolKeyTranslation> SupplyInformationDeliveryProtocolKeyTranslations { get; set; }
}