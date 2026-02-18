using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Repositories.Sales;

public sealed class GetAllSalesByClientNetIdQuery {
    public Guid ClientNetId { get; set; }

    public SaleLifeCycleType? SaleLifeCycleType { get; set; }

    public string Value { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public Guid UserNetId { get; set; }
}

public sealed class GetSalesRegisterByClientNetIdQuery {
    public Guid ClientNetId { get; set; }

    public SaleRegisterType? SaleRegisterType { get; set; }

    public string Value { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int Limit { get; set; }

    public int Offset { get; set; }

    public Guid UserNetId { get; set; }
}

public sealed class AddSaleFutureReservationQuery {
    public Guid ProductNetId { get; set; }

    public Guid ClientNetId { get; set; }

    public Guid SupplyOrderNetId { get; set; }

    public DateTime RemindDate { get; set; }

    public double Count { get; set; }
}
