using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Sales;

public sealed class OrderItem : EntityBase {
    public OrderItem() {
        ShiftStatuses = new HashSet<OrderItemBaseShiftStatus>();

        ProductReservations = new HashSet<ProductReservation>();

        OrderItemMerges = new HashSet<OrderItemMerged>();

        OldOrderItemMerges = new HashSet<OrderItemMerged>();

        OrderPackageItems = new HashSet<OrderPackageItem>();

        OrderItemMovements = new HashSet<OrderItemMovement>();

        SaleReturnItems = new HashSet<SaleReturnItem>();

        ProductLocations = new HashSet<ProductLocation>();

        ProductLocationsHistory = new HashSet<ProductLocationHistory>();

        TaxFreePackListOrderItems = new HashSet<TaxFreePackListOrderItem>();

        SadItems = new HashSet<SadItem>();

        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public double Qty { get; set; }

    public double OverLordQty { get; set; }

    public double UnpackedQty { get; set; }

    public double OrderedQty { get; set; }

    public double FromOfferQty { get; set; }

    public double InvoiceDocumentQty { get; set; }

    public double ChangedQty { get; set; }

    public double ReturnedQty { get; set; }

    public double TotalWeight { get; set; }

    public decimal OneTimeDiscount { get; set; }

    public decimal PricePerItem { get; set; }

    public decimal PricePerItemWithoutVat { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public decimal OverLordTotalAmount { get; set; }

    public decimal OverLordTotalAmountLocal { get; set; }
    public decimal TotalAmountEurToUah { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Discount { get; set; }

    public string OneTimeDiscountComment { get; set; }

    public string Comment { get; set; }

    public bool IsValidForCurrentSale { get; set; }

    public bool IsFromOffer { get; set; }

    public bool IsFromReSale { get; set; }

    public bool IsFromShiftedItem { get; set; }

    public OfferProcessingStatus OfferProcessingStatus { get; set; }

    public long ProductId { get; set; }

    public long? OrderId { get; set; }

    public long? OfferProcessingStatusChangedById { get; set; }

    public long? DiscountUpdatedById { get; set; }

    public long? UserId { get; set; }

    public long? ClientShoppingCartId { get; set; }

    public long? AssignedSpecificationId { get; set; }

    public decimal TotalVat { get; set; }

    public decimal Vat { get; set; }

    public long? MisplacedSaleId { get; set; }

    public bool IsMisplacedItem { get; set; }

    public bool IsClosed { get; set; }

    public User OfferProcessingStatusChangedBy { get; set; }

    public User DiscountUpdatedBy { get; set; }

    public User User { get; set; }

    public Order Order { get; set; }

    public Product Product { get; set; }

    public ClientShoppingCart ClientShoppingCart { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public ProductSpecification UkProductSpecification { get; set; }

    public ProductSpecification AssignedSpecification { get; set; }

    public MisplacedSale MisplacedSale { get; set; }

    public ICollection<OrderItemBaseShiftStatus> ShiftStatuses { get; set; }

    public ICollection<ProductReservation> ProductReservations { get; set; }

    public ICollection<OrderItemMerged> OrderItemMerges { get; set; }

    public ICollection<OrderItemMerged> OldOrderItemMerges { get; set; }

    public ICollection<OrderPackageItem> OrderPackageItems { get; set; }

    public ICollection<OrderItemMovement> OrderItemMovements { get; set; }

    public ICollection<SaleReturnItem> SaleReturnItems { get; set; }

    public ICollection<ProductLocation> ProductLocations { get; set; }
    public ICollection<ProductLocationHistory> ProductLocationsHistory { get; set; }

    public ICollection<TaxFreePackListOrderItem> TaxFreePackListOrderItems { get; set; }

    public ICollection<SadItem> SadItems { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }

    public List<OrderItem> OrderItemsGroupByProduct { get; set; }

    // For Mapping
    public Storage Storage { get; set; }
}