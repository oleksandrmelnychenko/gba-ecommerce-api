using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;

namespace GBA.Domain.Entities.Sales;

public sealed class HistoryInvoiceEdit : EntityBase {
    public HistoryInvoiceEdit() {
        OrderItemBaseShiftStatuses = new HashSet<OrderItemBaseShiftStatus>();
        ProductLocationHistory = new HashSet<ProductLocationHistory>();
    }

    public long SaleId { get; set; }
    public bool IsPrinted { get; set; }
    public bool IsDevelopment { get; set; }
    public bool ApproveUpdate { get; set; }
    public Sale Sale { get; set; }
    public double TotalRowsQty { get; set; }
    public ICollection<OrderItemBaseShiftStatus> OrderItemBaseShiftStatuses { get; set; }
    public ICollection<ProductLocationHistory> ProductLocationHistory { get; set; }
}