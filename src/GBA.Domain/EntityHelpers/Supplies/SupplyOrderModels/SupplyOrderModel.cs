using System;

namespace GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;

public sealed class SupplyOrderModel {
    public string Number { get; set; }

    public DateTime Created { get; set; }

    public DateTime FromDate { get; set; }

    public string InvNumber { get; set; }

    public DateTime InvDate { get; set; }

    public decimal TotalPrice { get; set; }

    public string Supplier { get; set; }

    public string Agreement { get; set; }

    public string Currency { get; set; }

    public double Qty { get; set; }

    public decimal AdditionalPrice { get; set; }

    public string Organization { get; set; }

    public string Placed { get; set; }

    public string Responsible { get; set; }
}