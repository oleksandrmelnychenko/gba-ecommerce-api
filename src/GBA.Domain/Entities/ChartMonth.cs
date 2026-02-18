using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class ChartMonth : EntityBase {
    public ChartMonth() {
        ChartMonthTranslations = new HashSet<ChartMonthTranslation>();
    }

    public string Name { get; set; }

    public int Number { get; set; }

    public ICollection<ChartMonthTranslation> ChartMonthTranslations { get; set; }
}