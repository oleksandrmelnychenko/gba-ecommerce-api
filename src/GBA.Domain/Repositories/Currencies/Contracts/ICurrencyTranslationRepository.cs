using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Currencies.Contracts;

public interface ICurrencyTranslationRepository {
    void Add(CurrencyTranslation currencyTranslation);

    void Add(IEnumerable<CurrencyTranslation> currencyTranslations);

    void Update(CurrencyTranslation currencyTranslation);

    void Update(IEnumerable<CurrencyTranslation> currencyTranslations);
}