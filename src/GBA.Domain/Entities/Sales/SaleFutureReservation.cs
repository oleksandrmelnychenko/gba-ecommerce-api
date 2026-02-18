using System;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.Sales;

public sealed class SaleFutureReservation : EntityBase {
    public long ProductId { get; set; }

    public long ClientId { get; set; }

    public long SupplyOrderId { get; set; }

    public DateTime RemindDate { get; set; }

    public double Count { get; set; }

    public Client Client { get; set; }

    public Product Product { get; set; }

    public SupplyOrder SupplyOrder { get; set; }
}