using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public class ChartMonthTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long ChartMonthId { get; set; }

    public virtual ChartMonth ChartMonth { get; set; }
}