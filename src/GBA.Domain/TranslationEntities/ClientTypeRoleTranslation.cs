using GBA.Domain.Entities.Clients;

namespace GBA.Domain.TranslationEntities;

public class ClientTypeRoleTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string Description { get; set; }

    public long ClientTypeRoleId { get; set; }

    public virtual ClientTypeRole ClientTypeRole { get; set; }
}