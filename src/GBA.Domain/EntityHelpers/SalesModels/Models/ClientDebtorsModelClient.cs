using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class ClientDebtorsModelClient {
    public ClientDebtorsModelClient() { }

    public ClientDebtorsModelClient(List<long> clientIds, int totalRowsQty) {
        ClientIds = clientIds;
        TotalRowsQty = totalRowsQty;
    }

    public List<long> ClientIds { get; set; }
    public int TotalRowsQty { get; set; }
}