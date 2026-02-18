using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Consignments;

public sealed class ConsignmentItemMovement : EntityBase {
    public bool IsIncomeMovement { get; set; }

    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public ConsignmentItemMovementType MovementType { get; set; }

    public long ConsignmentItemId { get; set; }

    public long? ProductIncomeItemId { get; set; }

    public long? DepreciatedOrderItemId { get; set; }

    public long? SupplyReturnItemId { get; set; }

    public long? OrderItemId { get; set; }

    public long? ReSaleItemId { get; set; }

    public long? ProductTransferItemId { get; set; }

    public long? OrderItemBaseShiftStatusId { get; set; }

    public long? TaxFreeItemId { get; set; }

    public long? SadItemId { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }

    public DepreciatedOrderItem DepreciatedOrderItem { get; set; }

    public SupplyReturnItem SupplyReturnItem { get; set; }

    public OrderItem OrderItem { get; set; }

    public ReSaleItem ReSaleItem { get; set; }

    public ProductTransferItem ProductTransferItem { get; set; }

    public OrderItemBaseShiftStatus OrderItemBaseShiftStatus { get; set; }

    public TaxFreeItem TaxFreeItem { get; set; }

    public SadItem SadItem { get; set; }
}