using System.Collections.Generic;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientTypeRole : EntityBase {
    public ClientTypeRole() {
        ClientInRoles = new HashSet<ClientInRole>();

        ClientTypeRoleTranslations = new HashSet<ClientTypeRoleTranslation>();

        PerfectClients = new HashSet<PerfectClient>();
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public long ClientTypeId { get; set; }

    public int OrderExpireDays { get; set; }

    public ClientType ClientType { get; set; }

    public ICollection<ClientInRole> ClientInRoles { get; set; }

    public ICollection<ClientTypeRoleTranslation> ClientTypeRoleTranslations { get; set; }

    public ICollection<PerfectClient> PerfectClients { get; set; }
}