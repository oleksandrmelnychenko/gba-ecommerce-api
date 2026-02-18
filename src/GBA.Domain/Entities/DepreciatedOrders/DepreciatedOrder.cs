using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.DepreciatedOrders;

public sealed class DepreciatedOrder : EntityBase {
    public DepreciatedOrder() {
        DepreciatedOrderItems = new HashSet<DepreciatedOrderItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long StorageId { get; set; }

    public long ResponsibleId { get; set; }

    public long OrganizationId { get; set; }

    public bool IsManagement { get; set; }

    public Storage Storage { get; set; }

    public User Responsible { get; set; }

    public Organization Organization { get; set; }

    public ICollection<DepreciatedOrderItem> DepreciatedOrderItems { get; set; }

    //Ignored

    public Currency Currency { get; set; }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }
}