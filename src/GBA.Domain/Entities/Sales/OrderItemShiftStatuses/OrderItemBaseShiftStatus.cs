using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.Entities.Sales.OrderItemShiftStatuses;

public class OrderItemBaseShiftStatus : EntityBase {
    public OrderItemBaseShiftStatus() {
        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();
    }

    public OrderItemShiftStatus ShiftStatus { get; set; }

    public string Comment { get; set; }

    public double Qty { get; set; }

    public double CurrentQty { get; set; }

    public long OrderItemId { get; set; }

    public long UserId { get; set; }

    public long? SaleId { get; set; }

    public long? HistoryInvoiceEditId { get; set; }

    public User User { get; set; }

    public OrderItem OrderItem { get; set; }

    public Sale Sale { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public long CurrentId { get; set; }
}