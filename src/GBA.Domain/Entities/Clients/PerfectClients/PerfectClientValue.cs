using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Clients.PerfectClients;

public sealed class PerfectClientValue : EntityBase {
    public PerfectClientValue() {
        ClientPerfectClients = new HashSet<ClientPerfectClient>();

        PerfectClientValueTranslations = new HashSet<PerfectClientValueTranslation>();
    }

    public string Value { get; set; }

    public bool IsSelected { get; set; }

    public long PerfectClientId { get; set; }

    public PerfectClient PerfectClient { get; set; }

    public ICollection<ClientPerfectClient> ClientPerfectClients { get; set; }

    public ICollection<PerfectClientValueTranslation> PerfectClientValueTranslations { get; set; }
}