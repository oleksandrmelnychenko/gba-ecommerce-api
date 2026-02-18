using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SadItem : EntityBase {
    public SadItem() {
        SadPalletItems = new HashSet<SadPalletItem>();

        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();
    }

    public double Qty { get; set; }

    public double UnpackedQty { get; set; }

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public decimal TotalAmountWithMargin { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal TotalVatAmountWithMargin { get; set; }

    public string Comment { get; set; }

    public long SadId { get; set; }

    public long? SupplyOrderUkraineCartItemId { get; set; }

    public long? OrderItemId { get; set; }

    public long? SupplierId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public Sad Sad { get; set; }

    public SupplyOrderUkraineCartItem SupplyOrderUkraineCartItem { get; set; }

    public OrderItem OrderItem { get; set; }

    public Client Supplier { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public ProductSpecification UkProductSpecification { get; set; }

    public ICollection<SadPalletItem> SadPalletItems { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }
}