using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public class OrganizationTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long OrganizationId { get; set; }

    public virtual Organization Organization { get; set; }
}