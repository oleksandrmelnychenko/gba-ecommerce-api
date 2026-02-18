using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Sales;

public sealed class Debt : EntityBase {
    public Debt() {
        ClientInDebts = new HashSet<ClientInDebt>();
    }

    public int Days { get; set; }

    public decimal Total { get; set; }

    public decimal EuroTotal { get; set; }

    public ICollection<ClientInDebt> ClientInDebts { get; set; }
}