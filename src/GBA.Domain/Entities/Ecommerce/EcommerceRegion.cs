using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Ecommerce;

public sealed class EcommerceRegion : EntityBase {
    public string NameUa { get; set; }

    public string NameRu { get; set; }

    public bool IsLocalPayment { get; set; }

    public ICollection<RetailClient> RetailClients { get; set; }
}