using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.TranslationEntities;

public class TransporterTypeTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long TransporterTypeId { get; set; }

    public virtual TransporterType TransporterType { get; set; }
}