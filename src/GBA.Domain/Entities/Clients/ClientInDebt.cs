using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientInDebt : EntityBase {
    public long ClientId { get; set; }

    public long AgreementId { get; set; }

    public long DebtId { get; set; }

    public long? SaleId { get; set; }

    public long? ReSaleId { get; set; }

    public Client Client { get; set; }

    public Agreement Agreement { get; set; }

    public Debt Debt { get; set; }

    public Sale Sale { get; set; }

    public ReSale ReSale { get; set; }
}