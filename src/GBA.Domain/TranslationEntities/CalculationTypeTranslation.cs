using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.TranslationEntities;

public class CalculationTypeTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long CalculationTypeId { get; set; }

    public virtual CalculationType CalculationType { get; set; }
}