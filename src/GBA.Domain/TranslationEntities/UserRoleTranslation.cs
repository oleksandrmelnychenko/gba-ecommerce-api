using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public class UserRoleTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long UserRoleId { get; set; }

    public virtual UserRole UserRole { get; set; }
}