using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.Consumables;

public sealed class ConsumablesOrder : EntityBase {
    public ConsumablesOrder() {
        ConsumablesOrderItems = new HashSet<ConsumablesOrderItem>();

        OutcomePaymentOrderConsumablesOrders = new HashSet<OutcomePaymentOrderConsumablesOrder>();

        ConsumablesOrderDocuments = new HashSet<ConsumablesOrderDocument>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public string OrganizationNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountWithoutVAT { get; set; }

    public bool IsPayed { get; set; }

    public DateTime OrganizationFromDate { get; set; }

    public long UserId { get; set; }

    public long? ConsumablesStorageId { get; set; }

    public long? SupplyPaymentTaskId { get; set; }

    public User User { get; set; }

    public SupplyOrganization ConsumableProductOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public ConsumablesStorage ConsumablesStorage { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }

    public ICollection<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }

    public ICollection<OutcomePaymentOrderConsumablesOrder> OutcomePaymentOrderConsumablesOrders { get; set; }

    public ICollection<ConsumablesOrderDocument> ConsumablesOrderDocuments { get; set; }
}