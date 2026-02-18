using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.TranslationEntities;

public class PerfectClientTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string Description { get; set; }

    public long PerfectClientId { get; set; }

    public virtual PerfectClient PerfectClient { get; set; }
}