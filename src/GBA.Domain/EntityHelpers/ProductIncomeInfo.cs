using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class ProductIncomeInfo {
    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public Client Supplier { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Storage Storage { get; set; }

    public DateTime FromDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal NetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public double RemainingQty { get; set; }

    public double TotalNetWeight { get; set; }
}