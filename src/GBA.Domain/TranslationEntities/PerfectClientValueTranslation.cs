using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.TranslationEntities;

public sealed class PerfectClientValueTranslation : TranslationEntityBase {
    public string Value { get; set; }

    public long PerfectClientValueId { get; set; }

    public PerfectClientValue PerfectClientValue { get; set; }
}