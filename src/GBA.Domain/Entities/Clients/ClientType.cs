using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientType : EntityBase {
    public ClientType() {
        ClientTypeTranslations = new HashSet<ClientTypeTranslation>();

        ClientTypeRoles = new HashSet<ClientTypeRole>();

        ClientInRoles = new HashSet<ClientInRole>();
    }

    public string Name { get; set; }

    public string ClientTypeIcon { get; set; }

    public bool AllowMultiple { get; set; }

    public ClientTypeType Type { get; set; }

    public ICollection<ClientTypeTranslation> ClientTypeTranslations { get; set; }

    public ICollection<ClientTypeRole> ClientTypeRoles { get; set; }

    public ICollection<ClientInRole> ClientInRoles { get; set; }
}