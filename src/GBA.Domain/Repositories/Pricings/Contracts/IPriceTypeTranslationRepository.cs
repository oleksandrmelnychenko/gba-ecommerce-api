using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IPriceTypeTranslationRepository {
    long Add(PriceTypeTranslation priceTypeTranslation);

    void Update(PriceTypeTranslation priceTypeTranslation);

    PriceTypeTranslation GetById(long id);

    PriceTypeTranslation GetByNetId(Guid netId);

    List<PriceTypeTranslation> GetAll();

    void Remove(Guid netId);
}