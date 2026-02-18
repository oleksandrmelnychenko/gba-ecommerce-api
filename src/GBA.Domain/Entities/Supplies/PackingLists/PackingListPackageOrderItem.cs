using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.PackingLists;

public sealed class PackingListPackageOrderItem : EntityBase {
    public PackingListPackageOrderItem() {
        ProductPlacements = new HashSet<ProductPlacement>();

        SupplyOrderUkraineCartItems = new HashSet<SupplyOrderUkraineCartItem>();

        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();

        DynamicProductPlacementRows = new HashSet<DynamicProductPlacementRow>();

        ProductIncomeItems = new HashSet<ProductIncomeItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public double Qty { get; set; }

    public double PlacedQty { get; set; }

    public double RemainingQty { get; set; }

    public double QtyDifferent { get; set; }

    public double UploadedQty { get; set; }

    public double ToOperationQty { get; set; }

    public string Reason { get; set; }

    public string Placement { get; set; }

    public long SupplyInvoiceOrderItemId { get; set; }

    public long? PackingListPackageId { get; set; }

    public long? PackingListId { get; set; }

    public bool IsPlaced { get; set; }

    public bool IsErrorInPlaced { get; set; }

    public bool IsReadyToPlaced { get; set; }

    public bool IsUpdated { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitPriceEur { get; set; }

    public decimal GrossUnitPriceEur { get; set; }

    public decimal UnitPriceUah { get; set; }

    public decimal UnitPriceEurWithVat { get; set; }

    public decimal AccountingGrossUnitPriceEur { get; set; }

    public decimal AccountingGeneralGrossUnitPriceEur { get; set; }

    public decimal ContainerUnitPriceEur { get; set; }

    public decimal AccountingContainerUnitPriceEur { get; set; }

    public decimal TotalPriceWithVatOne { get; set; }

    public decimal TotalPriceWithVatTwo { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal AccountingTotalNetPrice { get; set; }

    public decimal TotalNetPriceWithVat { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal AccountingTotalGrossPrice { get; set; }

    public decimal AccountingGeneralTotalGrossPrice { get; set; }

    public decimal TotalNetPriceEur { get; set; }

    public decimal TotalNetPriceWithVatEur { get; set; }

    public decimal TotalGrossPriceEur { get; set; }

    public decimal AccountingTotalGrossPriceEur { get; set; }

    public decimal AccountingGeneralTotalGrossPriceEur { get; set; }

    public decimal VatPercent { get; set; }

    public decimal VatAmount { get; set; }

    public decimal VatAmountEur { get; set; }

    public double GrossWeight { get; set; }

    public double NetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal ExchangeRateAmountUahToEur { get; set; }

    public decimal DeliveryPerItem { get; set; }

    public bool ProductIsImported { get; set; }

    public SupplyInvoiceOrderItem SupplyInvoiceOrderItem { get; set; }

    public PackingListPackage PackingListPackage { get; set; }

    public PackingList PackingList { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> SupplyOrderUkraineCartItems { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    public ICollection<DynamicProductPlacementRow> DynamicProductPlacementRows { get; set; }

    public ICollection<ProductIncomeItem> ProductIncomeItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }

    //Ignored field
    public Client Supplier { get; set; }

    public decimal DeliveryAmountUah { get; set; }

    public decimal DeliveryAmountEur { get; set; }

    public decimal TotalNetWithVat { get; set; }
}