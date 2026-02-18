using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.DebtorModels;

public sealed class ClientDebtorsModel {
    public ClientDebtorsModel() {
        ClientInDebtors = new List<ClientInDebtModel>();
    }

    public List<ClientInDebtModel> ClientInDebtors { get; }

    public int TotalQtyClients { get; set; }

    public int TotalMissedDays { get; set; }

    public decimal TotalRemainderDebtorsValue { get; set; }

    public decimal TotalOverdueDebtorsValue { get; set; }
}