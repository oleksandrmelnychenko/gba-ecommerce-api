using System.Collections.Generic;
using GBA.Domain.Entities.AllegroServices;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Products;

public sealed class Product : EntityBase {
    public Product() {
        PreOrders = new HashSet<PreOrder>();

        ProductCategories = new HashSet<ProductCategory>();

        ProductOriginalNumbers = new HashSet<ProductOriginalNumber>();

        ProductProductGroups = new HashSet<ProductProductGroup>();

        BaseAnalogueProducts = new HashSet<ProductAnalogue>();

        AnalogueProducts = new HashSet<ProductAnalogue>();

        BaseSetProducts = new HashSet<ProductSet>();

        ComponentProducts = new HashSet<ProductSet>();

        ProductPricings = new HashSet<ProductPricing>();

        ProductAvailabilities = new HashSet<ProductAvailability>();

        SupplyOrderItems = new HashSet<SupplyOrderItem>();

        ProductSpecifications = new HashSet<ProductSpecification>();

        SaleFutureReservations = new HashSet<SaleFutureReservation>();

        AllegroProductReservations = new HashSet<AllegroProductReservation>();

        ProductImages = new HashSet<ProductImage>();

        CalculatedPrices = new HashSet<CalculatedPricingsWithDiscounts>();

        ProductIncomes = new HashSet<ProductIncome>();

        ProductWriteOffRules = new HashSet<ProductWriteOffRule>();

        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();

        ActReconciliationItems = new HashSet<ActReconciliationItem>();

        DepreciatedOrderItems = new HashSet<DepreciatedOrderItem>();

        ProductTransferItems = new HashSet<ProductTransferItem>();

        SupplyReturnItems = new HashSet<SupplyReturnItem>();

        ProductPlacements = new HashSet<ProductPlacement>();

        SupplyOrderUkraineCartItems = new HashSet<SupplyOrderUkraineCartItem>();

        ProductCapitalizationItems = new HashSet<ProductCapitalizationItem>();

        ProductSlugs = new HashSet<ProductSlug>();

        ProductCarBrands = new HashSet<ProductCarBrand>();

        NextSearchedProducts = new List<Product>();

        ConsignmentItems = new HashSet<ConsignmentItem>();

        ReSaleItems = new HashSet<ReSaleItem>();

        SupplyInvoiceOrderItems = new HashSet<SupplyInvoiceOrderItem>();
    }

    public string VendorCode { get; set; }

    public string Name { get; set; }

    public string NameUA { get; set; }

    public string NamePL { get; set; }

    public string Description { get; set; }

    public string DescriptionUA { get; set; }

    public string DescriptionPL { get; set; }

    public string Size { get; set; }

    public string Notes { get; set; }

    public string NotesPL { get; set; }

    public string NotesUA { get; set; }

    public string PackingStandard { get; set; }

    public string Standard { get; set; }

    public string OrderStandard { get; set; }

    public string UCGFEA { get; set; }

    public string Volume { get; set; }

    public string Top { get; set; }

    public string SynonymsUA { get; set; }

    public string SynonymsPL { get; set; }

    public string RefId { get; set; }


    /* Fields for search only with replace special characters and Polish letters */

    public string SearchName { get; set; }

    public string SearchNameUA { get; set; }

    public string SearchNamePL { get; set; }

    public string SearchDescription { get; set; }

    public string SearchDescriptionUA { get; set; }

    public string SearchDescriptionPL { get; set; }

    public string SearchVendorCode { get; set; }

    public string SearchSize { get; set; }

    public string SearchSynonymsUA { get; set; }

    public string SearchSynonymsPL { get; set; }


    public double AvailableQtyUk { get; set; }

    public double AvailableQtyUkReSale { get; set; }

    public double AvailableQtyUkVAT { get; set; }

    public double AvailableQtyPl { get; set; }

    public double AvailableQtyRoad { get; set; }

    public double AvailableQtyPlVAT { get; set; }

    public double AvailableDefectiveQtyUk { get; set; }

    public double AvailableDefectiveQtyPl { get; set; }

    public double Weight { get; set; }

    public bool HasAnalogue { get; set; }

    public bool HasComponent { get; set; }

    public bool HasImage { get; set; }

    public bool IsForWeb { get; set; }

    public bool IsForSale { get; set; }

    public bool IsForZeroSale { get; set; }

    public string MainOriginalNumber { get; set; }

    public string Image { get; set; }

    public long MeasureUnitId { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal CurrentPriceEurToUah { get; set; }

    public decimal CurrentLocalPrice { get; set; }

    public decimal CurrentPriceReSale { get; set; }

    public decimal CurrentPriceReSaleEurToUah { get; set; }

    public decimal CurrentLocalPriceReSale { get; set; }

    public decimal CurrentWithVatPrice { get; set; }

    public decimal CurrentLocalWithVatPrice { get; set; }

    public byte[] ParentAmgId { get; set; } = null;

    public byte[] ParentFenixId { get; set; } = null;

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public MeasureUnit MeasureUnit { get; set; }

    public ProductSlug ProductSlug { get; set; }

    public ICollection<PreOrder> PreOrders { get; set; }

    public ICollection<ProductCategory> ProductCategories { get; set; }

    public ICollection<ProductOriginalNumber> ProductOriginalNumbers { get; set; }

    public ICollection<ProductProductGroup> ProductProductGroups { get; set; }

    public ICollection<ProductAnalogue> BaseAnalogueProducts { get; set; }

    public ICollection<ProductAnalogue> AnalogueProducts { get; set; }

    public ICollection<ProductSet> BaseSetProducts { get; set; }

    public ICollection<ProductSet> ComponentProducts { get; set; }

    public ICollection<ProductPricing> ProductPricings { get; set; }

    public ICollection<ProductAvailability> ProductAvailabilities { get; set; }

    public ICollection<SupplyOrderItem> SupplyOrderItems { get; set; }

    public ICollection<ProductSpecification> ProductSpecifications { get; set; }

    public ICollection<SaleFutureReservation> SaleFutureReservations { get; set; }

    public ICollection<AllegroProductReservation> AllegroProductReservations { get; set; }

    public ICollection<ProductImage> ProductImages { get; set; }

    public ICollection<CalculatedPricingsWithDiscounts> CalculatedPrices { get; set; }

    public ICollection<ProductIncome> ProductIncomes { get; set; }

    public ICollection<ProductWriteOffRule> ProductWriteOffRules { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    public ICollection<ActReconciliationItem> ActReconciliationItems { get; set; }

    public ICollection<DepreciatedOrderItem> DepreciatedOrderItems { get; set; }

    public ICollection<ProductTransferItem> ProductTransferItems { get; set; }

    public ICollection<SupplyReturnItem> SupplyReturnItems { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> SupplyOrderUkraineCartItems { get; set; }

    public ICollection<ProductCapitalizationItem> ProductCapitalizationItems { get; set; }

    public ICollection<ProductSlug> ProductSlugs { get; set; }

    public ICollection<ProductCarBrand> ProductCarBrands { get; set; }

    public ICollection<ConsignmentItem> ConsignmentItems { get; set; }

    public ICollection<ReSaleItem> ReSaleItems { get; set; }

    public ICollection<SupplyInvoiceOrderItem> SupplyInvoiceOrderItems { get; set; }

    public List<Product> NextSearchedProducts { get; set; }

    public string ProductGroupNames { get; set; }

    public string CurrencyCode { get; set; }
}