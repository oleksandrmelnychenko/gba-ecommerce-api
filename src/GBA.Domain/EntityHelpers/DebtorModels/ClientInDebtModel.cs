using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers.DebtorModels;

public sealed class ClientInDebtModel {
    public DateTime CreatedDebt { get; set; }

    public Guid ClientNetId { get; set; }

    public string ClientId { get; set; }

    public List<Debt> debts { get; set; }

    public List<Guid> ClientAgreementNetId { get; set; } = new();

    public decimal TotalDebtInDays { get; set; }

    public string RegionCode { get; set; }

    public string ClientName { get; set; }

    public string UserName { get; set; }

    public int MissedDays { get; set; }

    public decimal RemainderDebt { get; set; }

    public decimal OverdueDebt { get; set; }
}