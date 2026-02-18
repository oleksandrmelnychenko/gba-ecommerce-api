using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Agreements;

public sealed class CalculationType : EntityBase {
    public CalculationType() {
        CalculationTypeTranslations = new HashSet<CalculationTypeTranslation>();
    }

    public string Name { get; set; }

    public ICollection<CalculationTypeTranslation> CalculationTypeTranslations { get; set; }
}