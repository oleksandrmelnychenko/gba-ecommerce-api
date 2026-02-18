using GBA.Domain.Entities.Clients;

namespace GBA.Domain.TranslationEntities;

public class ClientTypeTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long ClientTypeId { get; set; }

    public virtual ClientType ClientType { get; set; }
}