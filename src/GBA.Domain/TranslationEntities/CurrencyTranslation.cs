using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public class CurrencyTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }
}