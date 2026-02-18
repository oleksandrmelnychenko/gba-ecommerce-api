using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInformationDeliveryProtocolKeyTranslationRepository {
    void Add(IEnumerable<SupplyInformationDeliveryProtocolKeyTranslation> keyTranslations);

    void Update(IEnumerable<SupplyInformationDeliveryProtocolKeyTranslation> keyTranslations);
}