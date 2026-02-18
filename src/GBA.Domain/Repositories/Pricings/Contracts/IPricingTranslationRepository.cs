using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IPricingTranslationRepository {
    long Add(PricingTranslation pricingTranslation);

    long Add(IEnumerable<PricingTranslation> pricingTranslations);

    void Update(PricingTranslation pricingTranslation);

    void Update(IEnumerable<PricingTranslation> pricingTranslations);

    PricingTranslation GetById(long id);

    PricingTranslation GetByNetId(Guid netId);

    List<PricingTranslation> GetAll();

    void Remove(Guid netId);
}