using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItem : EntityBase {
    public SupplyOrderUkraineCartItem() {
        TaxFreeItems = new HashSet<TaxFreeItem>();

        SadItems = new HashSet<SadItem>();

        SupplyOrderUkraineCartItemReservations = new HashSet<SupplyOrderUkraineCartItemReservation>();
    }

    public string Comment { get; set; }

    public double UploadedQty { get; set; }

    public double AvailableQty { get; set; }

    public double ReservedQty { get; set; }

    public double UnpackedQty { get; set; }

    public double NetWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public double PackageSize { get; set; }

    public double Coef { get; set; }

    public int MaxQtyPerTF { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitPriceLocal { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public SupplyOrderUkraineCartItemPriority ItemPriority { get; set; }

    public DateTime FromDate { get; set; }

    public bool IsRecommended { get; set; }

    public long ProductId { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? ResponsibleId { get; set; }

    public long? TaxFreePackListId { get; set; }

    public long? SupplierId { get; set; }

    public long? PackingListPackageOrderItemId { get; set; }

    public Product Product { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public User Responsible { get; set; }

    public TaxFreePackList TaxFreePackList { get; set; }

    public Client Supplier { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public ICollection<TaxFreeItem> TaxFreeItems { get; set; }

    public ICollection<SadItem> SadItems { get; set; }

    public ICollection<SupplyOrderUkraineCartItemReservation> SupplyOrderUkraineCartItemReservations { get; set; }
}