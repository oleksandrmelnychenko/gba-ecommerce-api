using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.TranslationEntities;

public class AgreementTypeTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long AgreementTypeId { get; set; }

    public virtual AgreementType AgreementType { get; set; }
}