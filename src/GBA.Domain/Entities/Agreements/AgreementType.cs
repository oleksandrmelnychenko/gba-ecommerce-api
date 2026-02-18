using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Agreements;

public sealed class AgreementType : EntityBase {
    public AgreementType() {
        AgreementTypeTranslations = new HashSet<AgreementTypeTranslation>();
    }

    public string Name { get; set; }

    public ICollection<AgreementTypeTranslation> AgreementTypeTranslations { get; set; }
}