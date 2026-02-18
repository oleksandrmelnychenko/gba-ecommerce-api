using System;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.ActProvidingServices;

public sealed class ActProvidingService : EntityBase {
    public bool IsAccounting { get; set; }

    public decimal Price { get; set; }

    public long UserId { get; set; }

    public DateTime FromDate { get; set; }

    public string Comment { get; set; }

    public string Number { get; set; }

    public User User { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }

    public BillOfLadingService AccountingBillOfLadingService { get; set; }

    public MergedService MergedService { get; set; }

    public MergedService AccountingMergedService { get; set; }

    public DeliveryExpense DeliveryExpense { get; set; }

    public DeliveryExpense AccountingDeliveryExpense { get; set; }
}