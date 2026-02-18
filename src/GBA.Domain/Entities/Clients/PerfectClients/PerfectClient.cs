using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Clients.PerfectClients;

public sealed class PerfectClient : EntityBase {
    public PerfectClient() {
        ClientPerfectClients = new HashSet<ClientPerfectClient>();

        Values = new HashSet<PerfectClientValue>();

        PerfectClientTranslations = new HashSet<PerfectClientTranslation>();
    }

    public string Lable { get; set; }

    public string Value { get; set; }

    public bool IsSelected { get; set; }

    public string Description { get; set; }

    public PerfectClientType Type { get; set; }

    public long? ClientTypeRoleId { get; set; }

    public ClientTypeRole ClientTypeRole { get; set; }

    public ICollection<ClientPerfectClient> ClientPerfectClients { get; set; }

    public ICollection<PerfectClientValue> Values { get; set; }

    public ICollection<PerfectClientTranslation> PerfectClientTranslations { get; set; }
}