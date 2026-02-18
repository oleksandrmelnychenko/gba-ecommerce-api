using GBA.Common.Helpers;
using GBA.Data.MapConfigurations;
using GBA.Data.TableMaps.ConcordContext;
using GBA.Data.TableMaps.ConcordContext.AccountingDocumentNames;
using GBA.Data.TableMaps.ConcordContext.Agreements;
using GBA.Data.TableMaps.ConcordContext.Auditing;
using GBA.Data.TableMaps.ConcordContext.Carriers;
using GBA.Data.TableMaps.ConcordContext.Clients;
using GBA.Data.TableMaps.ConcordContext.Clients.OrganizationClients;
using GBA.Data.TableMaps.ConcordContext.Clients.PackingMarkings;
using GBA.Data.TableMaps.ConcordContext.Clients.PerfectClients;
using GBA.Data.TableMaps.ConcordContext.ColumnItems;
using GBA.Data.TableMaps.ConcordContext.ConsignmentNoteSettings;
using GBA.Data.TableMaps.ConcordContext.Consignments;
using GBA.Data.TableMaps.ConcordContext.Consumables;
using GBA.Data.TableMaps.ConcordContext.CrossExchangeRates;
using GBA.Data.TableMaps.ConcordContext.Currencies;
using GBA.Data.TableMaps.ConcordContext.Dashboards;
using GBA.Data.TableMaps.ConcordContext.Delivery;
using GBA.Data.TableMaps.ConcordContext.DepreciatedOrders;
using GBA.Data.TableMaps.ConcordContext.Ecommerce;
using GBA.Data.TableMaps.ConcordContext.ExchangeRates;
using GBA.Data.TableMaps.ConcordContext.Filters;
using GBA.Data.TableMaps.ConcordContext.Measures;
using GBA.Data.TableMaps.ConcordContext.NumeratorMessages;
using GBA.Data.TableMaps.ConcordContext.Organizations;
using GBA.Data.TableMaps.ConcordContext.PaymentOrders;
using GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;
using GBA.Data.TableMaps.ConcordContext.PolishUserDetails;
using GBA.Data.TableMaps.ConcordContext.Pricings;
using GBA.Data.TableMaps.ConcordContext.Products;
using GBA.Data.TableMaps.ConcordContext.Products.Incomes;
using GBA.Data.TableMaps.ConcordContext.Products.Transfers;
using GBA.Data.TableMaps.ConcordContext.Regions;
using GBA.Data.TableMaps.ConcordContext.ReSales;
using GBA.Data.TableMaps.ConcordContext.SaleReturns;
using GBA.Data.TableMaps.ConcordContext.Sales;
using GBA.Data.TableMaps.ConcordContext.Sales.OrderPackages;
using GBA.Data.TableMaps.ConcordContext.Sales.Shipments;
using GBA.Data.TableMaps.ConcordContext.Supplies;
using GBA.Data.TableMaps.ConcordContext.Supplies.ActProvidingServices;
using GBA.Data.TableMaps.ConcordContext.Supplies.DeliveryProductProtocols;
using GBA.Data.TableMaps.ConcordContext.Supplies.Documents;
using GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;
using GBA.Data.TableMaps.ConcordContext.Supplies.PackingLists;
using GBA.Data.TableMaps.ConcordContext.Supplies.Returns;
using GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;
using GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine.Documents;
using GBA.Data.TableMaps.ConcordContext.Synchronizations;
using GBA.Data.TableMaps.ConcordContext.Transporters;
using GBA.Data.TableMaps.ConcordContext.UserManagement;
using GBA.Data.TableMaps.ConcordContext.UserNotifications;
using GBA.Data.TableMaps.ConcordContext.VatRates;
using GBA.Domain.AuditEntities;
using GBA.Domain.Entities;
using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.Documents;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Entities.ConsignmentNoteSettings;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.NumeratorMessages;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.PolishUserDetails;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Entities.UserNotifications;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.FilterEntities;
using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;

namespace GBA.Data;

public class ConcordContext : DbContext {
    public ConcordContext() { }

    public ConcordContext(DbContextOptions<ConcordContext> optionsBuilder) : base(optionsBuilder) { }
    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<UserRole> UserRole { get; set; }

    public virtual DbSet<UserRoleTranslation> UserRoleTranslation { get; set; }

    public virtual DbSet<Client> Client { get; set; }

    public virtual DbSet<ClientInRole> ClientInRole { get; set; }

    public virtual DbSet<ClientType> ClientType { get; set; }

    public virtual DbSet<ClientTypeRole> ClientTypeRole { get; set; }

    public virtual DbSet<ClientTypeRoleTranslation> ClientTypeRoleTranslation { get; set; }

    public virtual DbSet<ClientTypeTranslation> ClientTypeTranslation { get; set; }

    public virtual DbSet<PerfectClient> PerfectClient { get; set; }

    public virtual DbSet<PerfectClientTranslation> PerfectClientTranslation { get; set; }

    public virtual DbSet<PerfectClientValue> PerfectClientValue { get; set; }

    public virtual DbSet<PerfectClientValueTranslation> PerfectClientValueTranslation { get; set; }

    public virtual DbSet<ClientPerfectClient> ClientPerfectClient { get; set; }

    public virtual DbSet<ClientUserProfile> ClientUserProfile { get; set; }

    public virtual DbSet<ClientAgreement> ClientAgreement { get; set; }

    public virtual DbSet<ClientBalanceMovement> ClientBalanceMovement { get; set; }

    public virtual DbSet<ClientContractDocument> ClientContractDocument { get; set; }

    public virtual DbSet<ClientRegistrationTask> ClientRegistrationTask { get; set; }

    public virtual DbSet<Region> Region { get; set; }

    public virtual DbSet<RegionCode> RegionCode { get; set; }

    public virtual DbSet<Currency> Currency { get; set; }

    public virtual DbSet<CurrencyTranslation> CurrencyTranslation { get; set; }

    public virtual DbSet<Agreement> Agreement { get; set; }

    public virtual DbSet<AgreementType> AgreementType { get; set; }

    public virtual DbSet<CalculationType> CalculationType { get; set; }

    public virtual DbSet<AgreementTypeTranslation> AgreementTypeTranslation { get; set; }

    public virtual DbSet<CalculationTypeTranslation> CalculationTypeTranslation { get; set; }

    public virtual DbSet<Organization> Organization { get; set; }

    public virtual DbSet<OrganizationTranslation> OrganizationTranslation { get; set; }

    public virtual DbSet<PriceType> PriceType { get; set; }

    public virtual DbSet<PriceTypeTranslation> PriceTypeTranslation { get; set; }

    public virtual DbSet<Pricing> Pricing { get; set; }

    public virtual DbSet<PricingTranslation> PricingTranslation { get; set; }

    public virtual DbSet<ProviderPricing> ProviderPricing { get; set; }

    public virtual DbSet<MeasureUnit> MeasureUnit { get; set; }

    public virtual DbSet<MeasureUnitTranslation> MeasureUnitTranslation { get; set; }

    public virtual DbSet<Product> Product { get; set; }

    public virtual DbSet<ProductCategory> ProductCategory { get; set; }

    public virtual DbSet<ProductOriginalNumber> ProductOriginalNumber { get; set; }

    public virtual DbSet<ProductProductGroup> ProductProductGroup { get; set; }

    public virtual DbSet<ProductAnalogue> ProductAnalogue { get; set; }

    public virtual DbSet<ProductSet> ProductSet { get; set; }

    public virtual DbSet<ProductPricing> ProductPricing { get; set; }

    public virtual DbSet<PricingProductGroupDiscount> PricingProductGroupDiscount { get; set; }

    public virtual DbSet<ProductGroup> ProductGroup { get; set; }

    public virtual DbSet<ProductSubGroup> ProductSubGroup { get; set; }

    public virtual DbSet<ProductGroupDiscount> ProductGroupDiscount { get; set; }

    public virtual DbSet<ProductImage> ProductImage { get; set; }

    public virtual DbSet<Category> Category { get; set; }

    public virtual DbSet<OriginalNumber> OriginalNumber { get; set; }

    public virtual DbSet<FilterOperationItem> FilterOperation { get; set; }

    public virtual DbSet<FilterOperationItemTranslation> FilterOperationItemTranslation { get; set; }

    public virtual DbSet<FilterItem> FilterItem { get; set; }

    public virtual DbSet<FilterItemTranslation> FilterItemTranslation { get; set; }

    public virtual DbSet<ColumnItem> ColumnItem { get; set; }

    public virtual DbSet<ColumnItemTranslation> ColumnItemTranslation { get; set; }

    public virtual DbSet<ClientSubClient> ClientSubClient { get; set; }

    public virtual DbSet<ServicePayer> ServicePayer { get; set; }

    public virtual DbSet<AuditEntity> AuditEntity { get; set; }

    public virtual DbSet<AuditEntityProperty> AuditEntityProperty { get; set; }

    public virtual DbSet<AuditEntityPropertyNameTranslation> AuditEntityPropertyNameTranslation { get; set; }

    public virtual DbSet<Transporter> Transporter { get; set; }

    public virtual DbSet<TransporterType> TransporterType { get; set; }

    public virtual DbSet<TransporterTypeTranslation> TransporterTypeTranslation { get; set; }

    public virtual DbSet<Storage> Storage { get; set; }

    public virtual DbSet<Debt> Debt { get; set; }

    public virtual DbSet<ClientInDebt> ClientInDebt { get; set; }

    public virtual DbSet<ExchangeRate> ExchangeRate { get; set; }

    public virtual DbSet<ExchangeRateHistory> ExchangeRateHistory { get; set; }

    public virtual DbSet<Order> Order { get; set; }

    public virtual DbSet<PreOrder> PreOrder { get; set; }

    public virtual DbSet<OrderItem> OrderItem { get; set; }

    public virtual DbSet<OrderItemMovement> OrderItemMovement { get; set; }

    public virtual DbSet<UpdateDataCarrier> UpdateDataCarrier { get; set; }

    public virtual DbSet<WarehousesShipment> Shipment { get; set; }

    public virtual DbSet<Sale> Sale { get; set; }

    public virtual DbSet<SaleReturn> SaleReturn { get; set; }

    public virtual DbSet<SaleReturnItem> SaleReturnItem { get; set; }
    public virtual DbSet<SaleReturnItemProductPlacement> SaleReturnItemProductPlacement { get; set; }

    public virtual DbSet<BaseLifeCycleStatus> BaseLifeCycleStatus { get; set; }

    public virtual DbSet<BaseSalePaymentStatus> BaseSalePaymentStatus { get; set; }

    public virtual DbSet<SaleBaseShiftStatus> SaleBaseShiftStatus { get; set; }

    public virtual DbSet<OrderItemBaseShiftStatus> OrderItemBaseShiftStatus { get; set; }

    public virtual DbSet<SaleNumber> SaleNumber { get; set; }

    public virtual DbSet<DeliveryRecipientAddress> DeliveryRecipientAddress { get; set; }

    public virtual DbSet<SaleMerged> SaleMerged { get; set; }

    public virtual DbSet<SaleExchangeRate> SaleExchangeRates { get; set; }

    public virtual DbSet<SaleInvoiceDocument> SaleInvoiceDocument { get; set; }

    public virtual DbSet<SaleFutureReservation> SaleFutureReservation { get; set; }

    public virtual DbSet<SaleInvoiceNumber> SaleInvoiceNumber { get; set; }

    public virtual DbSet<OrderPackage> OrderPackage { get; set; }

    public virtual DbSet<HistoryInvoiceEdit> HistoryInvoiceEdit { get; set; }

    public virtual DbSet<OrderPackageUser> OrderPackageUser { get; set; }

    public virtual DbSet<OrderPackageItem> OrderPackageItem { get; set; }

    public virtual DbSet<DeliveryRecipient> DeliveryRecipient { get; set; }

    public virtual DbSet<ProductAvailability> ProductAvailability { get; set; }

    public virtual DbSet<ChartMonth> ChartMonth { get; set; }

    public virtual DbSet<ChartMonthTranslation> ChartMonthTranslation { get; set; }

    public virtual DbSet<ProductReservation> ProductReservation { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecification { get; set; }

    public virtual DbSet<UserScreenResolution> UserScreenResolution { get; set; }

    public virtual DbSet<OrderItemMerged> OrderItemMerged { get; set; }

    public virtual DbSet<SupplyOrder> SupplyOrder { get; set; }

    public virtual DbSet<SupplyOrderItem> SupplyOrderItem { get; set; }

    public virtual DbSet<SupplyOrderNumber> SupplyOrderNumber { get; set; }

    public virtual DbSet<SupplyInvoice> SupplyInvoice { get; set; }

    public virtual DbSet<SupplyProForm> SupplyProForm { get; set; }

    public virtual DbSet<SupplyPaymentTask> SupplyPaymentTask { get; set; }

    public virtual DbSet<SupplyPaymentTaskDocument> SupplyPaymentTaskDocument { get; set; }

    public virtual DbSet<InvoiceDocument> InvoiceDocument { get; set; }

    public virtual DbSet<PackingListDocument> PackingListDocument { get; set; }

    public virtual DbSet<ProFormDocument> ProFormDocument { get; set; }

    public virtual DbSet<SupplyOrderPaymentDeliveryProtocol> SupplyOrderPaymentDeliveryProtocol { get; set; }

    public virtual DbSet<SupplyOrderPaymentDeliveryProtocolKey> SupplyOrderPaymentDeliveryProtocolKey { get; set; }

    public virtual DbSet<ResponsibilityDeliveryProtocol> ResponsibilityDeliveryProtocol { get; set; }

    public virtual DbSet<SupplyInformationDeliveryProtocol> SupplyInformationDeliveryProtocol { get; set; }

    public virtual DbSet<SupplyInformationDeliveryProtocolKey> SupplyInformationDeliveryProtocolKey { get; set; }

    public virtual DbSet<SupplyInformationDeliveryProtocolKeyTranslation> SupplyInformationDeliveryProtocolKeyTranslation { get; set; }

    public virtual DbSet<PaymentDeliveryDocument> PaymentDeliveryDocument { get; set; }

    public virtual DbSet<SupplyDeliveryDocument> SupplyDeliveryDocument { get; set; }

    public virtual DbSet<SupplyOrderDeliveryDocument> SupplyOrderDeliveryDocument { get; set; }

    public virtual DbSet<SupplyOrganization> SupplyOrganization { get; set; }

    public virtual DbSet<SupplyOrganizationAgreement> SupplyOrganizationAgreement { get; set; }

    public virtual DbSet<SupplyOrganizationDocument> SupplyOrganizationDocument { get; set; }

    public virtual DbSet<SupplyServiceNumber> SupplyServiceNumber { get; set; }

    public virtual DbSet<Country> Country { get; set; }

    public virtual DbSet<ClientBankDetailIbanNo> ClientBankDetailIbanNo { get; set; }

    public virtual DbSet<ClientBankDetailAccountNumber> ClientBankDetailAccountNumber { get; set; }

    public virtual DbSet<ClientBankDetails> ClientBankDetails { get; set; }

    public virtual DbSet<TermsOfDelivery> TermsOfDelivery { get; set; }

    public virtual DbSet<PackingMarking> PackingMarking { get; set; }

    public virtual DbSet<PackingMarkingPayment> PackingMarkingPayment { get; set; }

    public virtual DbSet<ContainerService> ContainerService { get; set; }

    public virtual DbSet<SupplyOrderContainerService> SupplyOrderContainerService { get; set; }

    public virtual DbSet<BillOfLadingDocument> BillOfLadingDocument { get; set; }

    public virtual DbSet<WorkPermit> WorkPermit { get; set; }

    public virtual DbSet<WorkingContract> WorkingContract { get; set; }

    public virtual DbSet<ResidenceCard> ResidenceCard { get; set; }

    public virtual DbSet<UserDetails> UserDetails { get; set; }

    public virtual DbSet<CustomService> CustomService { get; set; }

    public virtual DbSet<PortWorkService> PortWorkService { get; set; }

    public virtual DbSet<TransportationService> TransportationService { get; set; }

    public virtual DbSet<CustomAgencyService> CustomAgencyService { get; set; }

    public virtual DbSet<PortCustomAgencyService> PortCustomAgencyService { get; set; }

    public virtual DbSet<PlaneDeliveryService> PlaneDeliveryService { get; set; }

    public virtual DbSet<VehicleDeliveryService> VehicleDeliveryService { get; set; }

    public virtual DbSet<CreditNoteDocument> CreditNoteDocument { get; set; }

    public virtual DbSet<CrossExchangeRate> CrossExchangeRate { get; set; }

    public virtual DbSet<CrossExchangeRateHistory> CrossExchangeRateHistory { get; set; }

    public virtual DbSet<SupplyOrderPolandPaymentDeliveryProtocol> SupplyOrderPolandPaymentDeliveryProtocol { get; set; }

    public virtual DbSet<ServiceDetailItem> ServiceDetailItem { get; set; }

    public virtual DbSet<ServiceDetailItemKey> ServiceDetailItemKey { get; set; }

    public virtual DbSet<PackingList> PackingList { get; set; }

    public virtual DbSet<PackingListPackage> PackingListItem { get; set; }

    public virtual DbSet<PackingListPackageOrderItem> PackingListPackageOrderItem { get; set; }

    public virtual DbSet<SupplyInvoiceOrderItem> SupplyInvoiceOrderItem { get; set; }

    public virtual DbSet<ClientShoppingCart> ClientShoppingCart { get; set; }

    public virtual DbSet<DashboardNodeModule> DashboardNodeModule { get; set; }

    public virtual DbSet<DashboardNode> DashboardNode { get; set; }

    public virtual DbSet<PaymentRegister> PaymentRegister { get; set; }

    public virtual DbSet<PaymentCurrencyRegister> PaymentCurrencyRegister { get; set; }

    public virtual DbSet<PaymentRegisterTransfer> PaymentRegisterTransfer { get; set; }

    public virtual DbSet<IncomePaymentOrder> IncomeCashOrder { get; set; }

    public virtual DbSet<IncomePaymentOrderSale> IncomePaymentOrderSale { get; set; }

    public virtual DbSet<OutcomePaymentOrder> OutcomePaymentOrder { get; set; }

    public virtual DbSet<OutcomePaymentOrderConsumablesOrder> OutcomePaymentOrderConsumablesOrder { get; set; }

    public virtual DbSet<OutcomePaymentOrderSupplyPaymentTask> OutcomePaymentOrderSupplyPaymentTask { get; set; }

    public virtual DbSet<PaymentRegisterCurrencyExchange> PaymentRegisterCurrencyExchange { get; set; }

    public virtual DbSet<CurrencyTrader> CurrencyTrader { get; set; }

    public virtual DbSet<CurrencyTraderExchangeRate> CurrencyTraderExchangeRate { get; set; }

    public virtual DbSet<PaymentMovement> PaymentMovement { get; set; }

    public virtual DbSet<PaymentMovementTranslation> PaymentMovementTranslation { get; set; }

    public virtual DbSet<PaymentMovementOperation> PaymentMovementOperation { get; set; }

    public virtual DbSet<PaymentCostMovement> PaymentCostMovement { get; set; }

    public virtual DbSet<PaymentCostMovementTranslation> PaymentCostMovementTranslation { get; set; }

    public virtual DbSet<PaymentCostMovementOperation> PaymentCostMovementOperation { get; set; }

    public virtual DbSet<AssignedPaymentOrder> AssignedPaymentOrder { get; set; }

    public virtual DbSet<ConsumableProduct> ConsumableProduct { get; set; }

    public virtual DbSet<ConsumableProductTranslation> ConsumableProductTranslation { get; set; }

    public virtual DbSet<ConsumableProductCategory> ConsumableProductCategory { get; set; }

    public virtual DbSet<ConsumableProductCategoryTranslation> ConsumableProductCategoryTranslation { get; set; }

    public virtual DbSet<ConsumablesOrder> ConsumablesOrder { get; set; }

    public virtual DbSet<ConsumablesOrderDocument> ConsumablesOrderDocument { get; set; }

    public virtual DbSet<ConsumablesOrderItem> ConsumablesOrderItem { get; set; }

    public virtual DbSet<ConsumablesStorage> ConsumablesStorage { get; set; }

    public virtual DbSet<DepreciatedConsumableOrder> DepreciatedConsumableOrder { get; set; }

    public virtual DbSet<DepreciatedConsumableOrderItem> DepreciatedConsumableOrderItem { get; set; }

    public virtual DbSet<CompanyCar> CompanyCar { get; set; }

    public virtual DbSet<CompanyCarRoadList> CompanyCarRoadList { get; set; }

    public virtual DbSet<CompanyCarRoadListDriver> CompanyCarRoadListDriver { get; set; }

    public virtual DbSet<CompanyCarFueling> CompanyCarFueling { get; set; }

    public virtual DbSet<Statham> Statham { get; set; }

    public virtual DbSet<StathamCar> StathamCar { get; set; }

    public virtual DbSet<Sad> Sad { get; set; }

    public virtual DbSet<SadDocument> SadDocument { get; set; }

    public virtual DbSet<TaxFreePackList> TaxFreePackList { get; set; }

    public virtual DbSet<TaxFree> TaxFree { get; set; }

    public virtual DbSet<TaxFreeDocument> TaxFreeDocument { get; set; }

    public virtual DbSet<ProductIncome> ProductIncome { get; set; }

    public virtual DbSet<ProductIncomeItem> ProductIncomeItem { get; set; }

    public virtual DbSet<ProductWriteOffRule> ProductWriteOffRule { get; set; }

    public virtual DbSet<SupplyOrderUkraine> SupplyOrderUkraine { get; set; }

    public virtual DbSet<SupplyOrderUkraineItem> SupplyOrderUkraineItem { get; set; }

    public virtual DbSet<ActReconciliation> ActReconciliation { get; set; }

    public virtual DbSet<ActReconciliationItem> ActReconciliationItem { get; set; }

    public virtual DbSet<DepreciatedOrder> DepreciatedOrder { get; set; }

    public virtual DbSet<DepreciatedOrderItem> DepreciatedOrderItem { get; set; }

    public virtual DbSet<ProductTransfer> ProductTransfer { get; set; }

    public virtual DbSet<ProductTransferItem> ProductTransferItem { get; set; }

    public virtual DbSet<SupplyReturn> SupplyReturn { get; set; }

    public virtual DbSet<SupplyReturnItem> SupplyReturnItem { get; set; }

    public virtual DbSet<ProductPlacement> ProductPlacement { get; set; }

    public virtual DbSet<ProductPlacementMovement> ProductPlacementMovement { get; set; }

    public virtual DbSet<ProductPlacementStorage> ProductPlacementStorage { get; set; }

    public virtual DbSet<ProductLocation> ProductLocation { get; set; }
    public virtual DbSet<ProductLocationHistory> ProductLocationHistory { get; set; }

    public virtual DbSet<DocumentMonth> DocumentMonth { get; set; }

    public virtual DbSet<ShipmentList> ShipmentList { get; set; }

    public virtual DbSet<ShipmentListItem> ShipmentListItem { get; set; }

    public virtual DbSet<SupplyOrderUkraineCartItem> SupplyOrderUkraineCartItem { get; set; }

    public virtual DbSet<TaxFreeItem> TaxFreeItem { get; set; }

    public virtual DbSet<DynamicProductPlacementColumn> DynamicProductPlacementColumn { get; set; }

    public virtual DbSet<DynamicProductPlacementRow> DynamicProductPlacementRow { get; set; }

    public virtual DbSet<DynamicProductPlacement> DynamicProductPlacement { get; set; }

    public virtual DbSet<SadItem> SadItem { get; set; }

    public virtual DbSet<OrganizationClient> OrganizationClient { get; set; }

    public virtual DbSet<OrganizationClientAgreement> OrganizationClientAgreement { get; set; }

    public virtual DbSet<Incoterm> Incoterm { get; set; }

    public virtual DbSet<TaxFreePackListOrderItem> TaxFreePackListOrderItem { get; set; }

    public virtual DbSet<StathamPassport> StathamPassport { get; set; }

    public virtual DbSet<SadPalletType> SadPalletType { get; set; }

    public virtual DbSet<SadPallet> SadPallet { get; set; }

    public virtual DbSet<SadPalletItem> SadPalletItem { get; set; }

    public virtual DbSet<TaxInspection> TaxInspection { get; set; }

    public virtual DbSet<ProductCapitalization> ProductCapitalization { get; set; }

    public virtual DbSet<ProductCapitalizationItem> ProductCapitalizationItem { get; set; }

    public virtual DbSet<MergedService> MergedService { get; set; }

    public virtual DbSet<SupplyOrderUkrainePaymentDeliveryProtocolKey> SupplyOrderUkrainePaymentDeliveryProtocolKey { get; set; }

    public virtual DbSet<SupplyOrderUkrainePaymentDeliveryProtocol> SupplyOrderUkrainePaymentDeliveryProtocol { get; set; }

    public virtual DbSet<ExpiredBillUserNotification> ExpiredBillUserNotification { get; set; }

    public virtual DbSet<AdvancePayment> AdvancePayment { get; set; }

    public virtual DbSet<ProductSlug> ProductSlug { get; set; }

    public virtual DbSet<CarBrand> CarBrand { get; set; }

    public virtual DbSet<ProductCarBrand> ProductCarBrand { get; set; }

    public virtual DbSet<OrderProductSpecification> OrderProductSpecification { get; set; }

    public virtual DbSet<SupportVideo> SupportVideo { get; set; }

    public virtual DbSet<AccountingDocumentName> AccountingDocumentName { get; set; }

    public virtual DbSet<SaleReturnItemStatusName> SaleReturnItemStatusName { get; set; }

    public virtual DbSet<Consignment> Consignment { get; set; }

    public virtual DbSet<ConsignmentItem> ConsignmentItem { get; set; }

    public virtual DbSet<ConsignmentItemMovement> ConsignmentItemMovement { get; set; }

    public virtual DbSet<ConsignmentItemMovementTypeName> ConsignmentItemMovementTypeName { get; set; }

    public virtual DbSet<SupplyOrderUkraineCartItemReservation> SupplyOrderUkraineCartItemReservation { get; set; }

    public virtual DbSet<TaxAccountingScheme> TaxAccountingScheme { get; set; }

    public virtual DbSet<AgreementTypeCivilCode> AgreementTypeCivilCode { get; set; }

    public virtual DbSet<CountSaleMessage> CountSaleMessage { get; set; }

    public virtual DbSet<SaleMessageNumerator> SaleMessageNumerator { get; set; }
    public virtual DbSet<ProductPlacementHistory> ProductPlacementHistory { get; set; }

    public virtual DbSet<SupplyOrderUkraineCartItemReservationProductPlacement> SupplyOrderUkraineCartItemReservationProductPlacement { get; set; }

    public virtual DbSet<DataSyncOperation> DataSyncOperation { get; set; }

    public virtual DbSet<VehicleService> VehicleService { get; set; }

    public virtual DbSet<SupplyOrderVehicleService> SupplyOrderVehicleService { get; set; }

    public virtual DbSet<GovExchangeRate> GovExchangeRate { get; set; }

    public virtual DbSet<GovExchangeRateHistory> GovExchangeRateHistory { get; set; }

    public virtual DbSet<GovCrossExchangeRate> GovCrossExchangeRate { get; set; }

    public virtual DbSet<GovCrossExchangeRateHistory> GovCrossExchangeRateHistory { get; set; }

    public virtual DbSet<DeliveryProductProtocol> DeliveryProductProtocol { get; set; }

    public virtual DbSet<BillOfLadingService> BillOfLadingService { get; set; }

    public virtual DbSet<SupplyInvoiceMergedService> SupplyInvoiceMergedService { get; set; }

    public virtual DbSet<SupplyInvoiceBillOfLadingService> SupplyInvoiceBillOfLadingService { get; set; }

    public virtual DbSet<DeliveryProductProtocolDocument> DeliveryProductProtocolDocument { get; set; }

    public virtual DbSet<DeliveryProductProtocolNumber> DeliveryProductProtocolNumber { get; set; }

    public virtual DbSet<ReSaleItem> ReSaleItem { get; set; }

    public virtual DbSet<ReSale> ReSale { get; set; }

    public virtual DbSet<SupplyInvoiceDeliveryDocument> SupplyInvoiceDeliveryDocument { get; set; }

    public virtual DbSet<SupplyInformationTask> SupplyInformationTask { get; set; }

    public virtual DbSet<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyService { get; set; }

    public virtual DbSet<ActProvidingServiceDocument> ActProvidingServiceDocument { get; set; }

    public virtual DbSet<SupplyServiceAccountDocument> SupplyServiceAccountDocument { get; set; }

    public virtual DbSet<ActProvidingService> ActProvidingService { get; set; }

    public virtual DbSet<SupplyOrderUkraineDocument> SupplyOrderUkraineDocument { get; set; }

    public virtual DbSet<ConsignmentNoteSetting> ConsignmentNoteSettings { get; set; }

    public virtual DbSet<VatRate> VatRate { get; set; }

    public virtual DbSet<ReSaleAvailability> ReSaleAvailability { get; set; }

    public virtual DbSet<EcommercePage> EcommercePages { get; set; }

    public virtual DbSet<EcommerceContacts> EcommerceContacts { get; set; }

    public virtual DbSet<EcommerceContactInfo> EcommerceContactInfo { get; set; }

    public virtual DbSet<SeoPage> SeoPages { get; set; }

    public virtual DbSet<EcommerceDefaultPricing> EcommerceDefaultPricings { get; set; }

    public virtual DbSet<RetailPaymentTypeTranslate> RetailPaymentTypeTranslates { get; set; }

    public virtual DbSet<RetailClient> RetailClients { get; set; }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<EcommerceRegion> EcommerceRegions { get; set; }

    public virtual DbSet<MisplacedSale> MisplacedSales { get; set; }

    public virtual DbSet<RetailClientPaymentImage> RetailClientPaymentImages { get; set; }

    public virtual DbSet<RetailClientPaymentImageItem> RetailClientPaymentImageItems { get; set; }

    public virtual DbSet<ClientGroup> ClientGroups { get; set; }

    public virtual DbSet<ClientWorkplace> ClientWorkplaces { get; set; }

    public virtual DbSet<Workplace> Workplaces { get; set; }

    public virtual DbSet<WorkplaceClientAgreement> WorkplaceClientAgreements { get; set; }

    public virtual DbSet<RetailPaymentStatus> RetailPaymentStatus { get; set; }

    public virtual DbSet<AccountingOperationName> AccountingOperationNames { get; set; }

    public virtual DbSet<UserRoleDashboardNode> UserRoleDashboardNodes { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RolePermission> RolePermission { get; set; }

    public virtual DbSet<DeliveryExpense> DeliveryExpenses { get; set; }

    public virtual DbSet<CustomersOwnTtn> CustomersOwnTtns { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlServer(ConfigurationManager.LocalDatabaseConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.AddConfiguration(new UserMap());

        modelBuilder.AddConfiguration(new UserRoleMap());

        modelBuilder.AddConfiguration(new UserRoleTranslationMap());

        modelBuilder.AddConfiguration(new ClientMap());

        modelBuilder.AddConfiguration(new ClientInRoleMap());

        modelBuilder.AddConfiguration(new ClientTypeMap());

        modelBuilder.AddConfiguration(new ClientTypeRoleMap());

        modelBuilder.AddConfiguration(new ClientTypeRoleTranslationMap());

        modelBuilder.AddConfiguration(new PerfectClientMap());

        modelBuilder.AddConfiguration(new PerfectClientTranslationMap());

        modelBuilder.AddConfiguration(new PerfectClientValueMap());

        modelBuilder.AddConfiguration(new PerfectClientValueTranslationMap());

        modelBuilder.AddConfiguration(new ClientPerfectClientMap());

        modelBuilder.AddConfiguration(new ClientUserProfileMap());

        modelBuilder.AddConfiguration(new ClientAgreementMap());

        modelBuilder.AddConfiguration(new ClientBalanceMovementMap());

        modelBuilder.AddConfiguration(new ClientContractDocumentMap());

        modelBuilder.AddConfiguration(new ClientSubClientMap());

        modelBuilder.AddConfiguration(new ClientRegistrationTaskMap());

        modelBuilder.AddConfiguration(new ServicePayerMap());

        modelBuilder.AddConfiguration(new RegionMap());

        modelBuilder.AddConfiguration(new RegionCodeMap());

        modelBuilder.AddConfiguration(new CurrencyMap());

        modelBuilder.AddConfiguration(new CurrencyTranslationMap());

        modelBuilder.AddConfiguration(new AgreementTypeMap());

        modelBuilder.AddConfiguration(new CalculationTypeMap());

        modelBuilder.AddConfiguration(new AgreementMap());

        modelBuilder.AddConfiguration(new ClientTypeTranslationMap());

        modelBuilder.AddConfiguration(new AgreementTypeTranslationMap());

        modelBuilder.AddConfiguration(new CalculationTypeTranslationMap());

        modelBuilder.AddConfiguration(new OrganizationMap());

        modelBuilder.AddConfiguration(new OrganizationTranslationMap());

        modelBuilder.AddConfiguration(new PriceTypeMap());

        modelBuilder.AddConfiguration(new PriceTypeTranslationMap());

        modelBuilder.AddConfiguration(new PricingMap());

        modelBuilder.AddConfiguration(new PricingTranslationMap());

        modelBuilder.AddConfiguration(new ProviderPricingMap());

        modelBuilder.AddConfiguration(new MeasureUnitMap());

        modelBuilder.AddConfiguration(new MeasureUnitTranslationMap());

        modelBuilder.AddConfiguration(new ProductMap());

        modelBuilder.AddConfiguration(new ProductCategoryMap());

        modelBuilder.AddConfiguration(new ProductOriginalNumberMap());

        modelBuilder.AddConfiguration(new ProductProductGroupMap());

        modelBuilder.AddConfiguration(new ProductAnalogueMap());

        modelBuilder.AddConfiguration(new ProductSetMap());

        modelBuilder.AddConfiguration(new ProductPricingMap());

        modelBuilder.AddConfiguration(new PricingProductGroupDiscountMap());

        modelBuilder.AddConfiguration(new ProductGroupMap());

        modelBuilder.AddConfiguration(new ProductSubGroupMap());

        modelBuilder.AddConfiguration(new ProductGroupDiscountMap());

        modelBuilder.AddConfiguration(new ProductImageMap());

        modelBuilder.AddConfiguration(new CategoryMap());

        modelBuilder.AddConfiguration(new OriginalNumberMap());

        modelBuilder.AddConfiguration(new FilterOperationItemMap());

        modelBuilder.AddConfiguration(new FilterOperationItemTranslationMap());

        modelBuilder.AddConfiguration(new FilterItemMap());

        modelBuilder.AddConfiguration(new FilterItemTranslationMap());

        modelBuilder.AddConfiguration(new ColumnItemMap());

        modelBuilder.AddConfiguration(new ColumnItemTranslationMap());

        modelBuilder.AddConfiguration(new AuditEntityMap());

        modelBuilder.AddConfiguration(new AuditEntityPropertyMap());

        modelBuilder.AddConfiguration(new AuditEntityPropertyNameTranslationMap());

        modelBuilder.AddConfiguration(new TransporterMap());

        modelBuilder.AddConfiguration(new TransporterTypeMap());

        modelBuilder.AddConfiguration(new TransporterTypeTranslationMap());

        modelBuilder.AddConfiguration(new StorageMap());

        modelBuilder.AddConfiguration(new DebtMap());

        modelBuilder.AddConfiguration(new ClientInDebtMap());

        modelBuilder.AddConfiguration(new ExchangeRateMap());

        modelBuilder.AddConfiguration(new ExchangeRateHistoryMap());

        modelBuilder.AddConfiguration(new OrderMap());

        modelBuilder.AddConfiguration(new PreOrderMap());

        modelBuilder.AddConfiguration(new OrderItemMap());

        modelBuilder.AddConfiguration(new OrderItemMovementMap());

        modelBuilder.AddConfiguration(new SaleMap());

        modelBuilder.AddConfiguration(new UpdateDataCarrierMap());

        modelBuilder.AddConfiguration(new ShipmentEditMap());

        modelBuilder.AddConfiguration(new SaleReturnMap());

        modelBuilder.AddConfiguration(new SaleReturnItemMap());

        modelBuilder.AddConfiguration(new SaleReturnItemProductPlacementMap());

        modelBuilder.AddConfiguration(new BaseLifeCycleStatusMap());

        modelBuilder.AddConfiguration(new BaseSalePaymentStatusMap());

        modelBuilder.AddConfiguration(new SaleBaseShiftStatusMap());

        modelBuilder.AddConfiguration(new OrderItemBaseShiftStatusMap());

        modelBuilder.AddConfiguration(new SaleNumberMap());

        modelBuilder.AddConfiguration(new SaleInvoiceDocumentMap());

        modelBuilder.AddConfiguration(new SaleFutureReservationMap());

        modelBuilder.AddConfiguration(new SaleMergedMap());

        modelBuilder.AddConfiguration(new DeliveryRecipientAddressMap());

        modelBuilder.AddConfiguration(new SaleExchangeRateMap());

        modelBuilder.AddConfiguration(new HistoryInvoiceEditMap());

        modelBuilder.AddConfiguration(new SaleInvoiceNumberMap());

        modelBuilder.AddConfiguration(new OrderPackageMap());

        modelBuilder.AddConfiguration(new OrderPackageUserMap());

        modelBuilder.AddConfiguration(new OrderPackageItemMap());

        modelBuilder.AddConfiguration(new DeliveryRecipientMap());

        modelBuilder.AddConfiguration(new ProductAvailabilityMap());

        modelBuilder.AddConfiguration(new ChartMonthMap());

        modelBuilder.AddConfiguration(new ChartMonthTranslationMap());

        modelBuilder.AddConfiguration(new ProductReservationMap());

        modelBuilder.AddConfiguration(new ProductSpecificationMap());

        modelBuilder.AddConfiguration(new UserScreenResolutionMap());

        modelBuilder.AddConfiguration(new OrderItemMergedMap());

        modelBuilder.AddConfiguration(new InvoiceDocumentMap());

        modelBuilder.AddConfiguration(new PackingListDocumentMap());

        modelBuilder.AddConfiguration(new ProFormDocumentMap());

        modelBuilder.AddConfiguration(new SupplyOrderPaymentDeliveryProtocolMap());

        modelBuilder.AddConfiguration(new SupplyOrderPaymentDeliveryProtocolKeyMap());

        modelBuilder.AddConfiguration(new ResponsibilityDeliveryProtocolMap());

        modelBuilder.AddConfiguration(new SupplyInvoiceMap());

        modelBuilder.AddConfiguration(new SupplyOrderMap());

        modelBuilder.AddConfiguration(new SupplyOrderItemMap());

        modelBuilder.AddConfiguration(new SupplyOrderNumberMap());

        modelBuilder.AddConfiguration(new SupplyPaymentTaskMap());

        modelBuilder.AddConfiguration(new SupplyPaymentTaskDocumentMap());

        modelBuilder.AddConfiguration(new SupplyProFormMap());

        modelBuilder.AddConfiguration(new SupplyInformationDeliveryProtocolMap());

        modelBuilder.AddConfiguration(new SupplyInformationDeliveryProtocolKeyMap());

        modelBuilder.AddConfiguration(new SupplyInformationDeliveryProtocolKeyTranslationMap());

        modelBuilder.AddConfiguration(new PaymentDeliveryDocumentMap());

        modelBuilder.AddConfiguration(new SupplyDeliveryDocumentMap());

        modelBuilder.AddConfiguration(new SupplyOrderDeliveryDocumentMap());

        modelBuilder.AddConfiguration(new SupplyOrganizationMap());

        modelBuilder.AddConfiguration(new SupplyOrganizationAgreementMap());

        modelBuilder.AddConfiguration(new SupplyOrganizationDocumentMap());

        modelBuilder.AddConfiguration(new SupplyServiceNumberMap());

        modelBuilder.AddConfiguration(new CountryMap());

        modelBuilder.AddConfiguration(new ClientBankDetailIbanNoMap());

        modelBuilder.AddConfiguration(new ClientBankDetailAccountNumberMap());

        modelBuilder.AddConfiguration(new ClientBankDetailsMap());

        modelBuilder.AddConfiguration(new TermsOfDeliveryMap());

        modelBuilder.AddConfiguration(new PackingMarkingMap());

        modelBuilder.AddConfiguration(new PackingMarkingPaymentMap());

        modelBuilder.AddConfiguration(new ContainerServiceMap());

        modelBuilder.AddConfiguration(new SupplyOrderContainerServiceMap());

        modelBuilder.AddConfiguration(new BillOfLadingDocumentMap());

        modelBuilder.AddConfiguration(new WorkPermitMap());

        modelBuilder.AddConfiguration(new WorkingContractMap());

        modelBuilder.AddConfiguration(new ResidenceCardMap());

        modelBuilder.AddConfiguration(new UserDetailsMap());

        modelBuilder.AddConfiguration(new CustomServiceMap());

        modelBuilder.AddConfiguration(new PortWorkServiceMap());

        modelBuilder.AddConfiguration(new TransportationServiceMap());

        modelBuilder.AddConfiguration(new CustomAgencyServiceMap());

        modelBuilder.AddConfiguration(new PortCustomAgencyServiceMap());

        modelBuilder.AddConfiguration(new PlaneDeliveryServiceMap());

        modelBuilder.AddConfiguration(new VehicleDeliveryServiceMap());

        modelBuilder.AddConfiguration(new CreditNoteDocumentMap());

        modelBuilder.AddConfiguration(new CrossExchangeRateMap());

        modelBuilder.AddConfiguration(new CrossExchangeRateHistoryMap());

        modelBuilder.AddConfiguration(new SupplyOrderPolandPaymentDeliveryProtocolMap());

        modelBuilder.AddConfiguration(new ServiceDetailItemMap());

        modelBuilder.AddConfiguration(new ServiceDetailItemKeyMap());

        modelBuilder.AddConfiguration(new PackingListMap());

        modelBuilder.AddConfiguration(new PackingListPackageMap());

        modelBuilder.AddConfiguration(new PackingListPackageOrderItemMap());

        modelBuilder.AddConfiguration(new SupplyInvoiceOrderItemMap());

        modelBuilder.AddConfiguration(new ClientShoppingCartMap());

        modelBuilder.AddConfiguration(new DashboardNodeModuleMap());

        modelBuilder.AddConfiguration(new DashboardNodeMap());

        modelBuilder.AddConfiguration(new PaymentRegisterMap());

        modelBuilder.AddConfiguration(new PaymentCurrencyRegisterMap());

        modelBuilder.AddConfiguration(new CurrencyTraderMap());

        modelBuilder.AddConfiguration(new CurrencyTraderExchangeRateMap());

        modelBuilder.AddConfiguration(new PaymentRegisterTransferMap());

        modelBuilder.AddConfiguration(new IncomePaymentOrderMap());

        modelBuilder.AddConfiguration(new IncomePaymentOrderSaleMap());

        modelBuilder.AddConfiguration(new OutcomePaymentOrderMap());

        modelBuilder.AddConfiguration(new OutcomePaymentOrderConsumablesOrderMap());

        modelBuilder.AddConfiguration(new OutcomePaymentOrderSupplyPaymentTaskMap());

        modelBuilder.AddConfiguration(new PaymentRegisterCurrencyExchangeMap());

        modelBuilder.AddConfiguration(new PaymentMovementMap());

        modelBuilder.AddConfiguration(new PaymentMovementTranslationMap());

        modelBuilder.AddConfiguration(new PaymentMovementOperationMap());

        modelBuilder.AddConfiguration(new PaymentCostMovementMap());

        modelBuilder.AddConfiguration(new PaymentCostMovementTranslationMap());

        modelBuilder.AddConfiguration(new PaymentCostMovementOperationMap());

        modelBuilder.AddConfiguration(new AssignedPaymentOrderMap());

        modelBuilder.AddConfiguration(new ConsumableProductMap());

        modelBuilder.AddConfiguration(new ConsumableProductTranslationMap());

        modelBuilder.AddConfiguration(new ConsumableProductCategoryMap());

        modelBuilder.AddConfiguration(new ConsumableProductCategoryTranslationMap());

        modelBuilder.AddConfiguration(new ConsumablesOrderMap());

        modelBuilder.AddConfiguration(new ConsumablesOrderDocumentMap());

        modelBuilder.AddConfiguration(new ConsumablesOrderItemMap());

        modelBuilder.AddConfiguration(new ConsumablesStorageMap());

        modelBuilder.AddConfiguration(new DepreciatedConsumableOrderMap());

        modelBuilder.AddConfiguration(new DepreciatedConsumableOrderItemMap());

        modelBuilder.AddConfiguration(new CompanyCarMap());

        modelBuilder.AddConfiguration(new CompanyCarRoadListMap());

        modelBuilder.AddConfiguration(new CompanyCarRoadListDriverMap());

        modelBuilder.AddConfiguration(new CompanyCarFuelingMap());

        modelBuilder.AddConfiguration(new StathamMap());

        modelBuilder.AddConfiguration(new StathamCarMap());

        modelBuilder.AddConfiguration(new SadMap());

        modelBuilder.AddConfiguration(new SadDocumentMap());

        modelBuilder.AddConfiguration(new TaxFreePackListMap());

        modelBuilder.AddConfiguration(new TaxFreeMap());

        modelBuilder.AddConfiguration(new TaxFreeDocumentMap());

        modelBuilder.AddConfiguration(new ProductIncomeMap());

        modelBuilder.AddConfiguration(new ProductIncomeItemMap());

        modelBuilder.AddConfiguration(new ProductWriteOffRuleMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineItemMap());

        modelBuilder.AddConfiguration(new ActReconciliationMap());

        modelBuilder.AddConfiguration(new ActReconciliationItemMap());

        modelBuilder.AddConfiguration(new DepreciatedOrderMap());

        modelBuilder.AddConfiguration(new DepreciatedOrderItemMap());

        modelBuilder.AddConfiguration(new ProductTransferMap());

        modelBuilder.AddConfiguration(new ProductTransferItemMap());

        modelBuilder.AddConfiguration(new SupplyReturnMap());

        modelBuilder.AddConfiguration(new SupplyReturnItemMap());

        modelBuilder.AddConfiguration(new ProductPlacementMap());

        modelBuilder.AddConfiguration(new ProductPlacementHistoryMap());

        modelBuilder.AddConfiguration(new ProductPlacementMovementMap());

        modelBuilder.AddConfiguration(new ProductPlacementStorageMap());

        modelBuilder.AddConfiguration(new ProductLocationMap());

        modelBuilder.AddConfiguration(new ProductLocationHistoryMap());

        modelBuilder.AddConfiguration(new DocumentMonthMap());

        modelBuilder.AddConfiguration(new ShipmentListMap());

        modelBuilder.AddConfiguration(new ShipmentListItemMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineCartItemMap());

        modelBuilder.AddConfiguration(new TaxFreeItemMap());

        modelBuilder.AddConfiguration(new DynamicProductPlacementColumnMap());

        modelBuilder.AddConfiguration(new DynamicProductPlacementRowMap());

        modelBuilder.AddConfiguration(new DynamicProductPlacementMap());

        modelBuilder.AddConfiguration(new SadItemMap());

        modelBuilder.AddConfiguration(new OrganizationClientMap());

        modelBuilder.AddConfiguration(new OrganizationClientAgreementMap());

        modelBuilder.AddConfiguration(new IncotermMap());

        modelBuilder.AddConfiguration(new TaxFreePackListOrderItemMap());

        modelBuilder.AddConfiguration(new StathamPassportMap());

        modelBuilder.AddConfiguration(new SadPalletTypeMap());

        modelBuilder.AddConfiguration(new SadPalletMap());

        modelBuilder.AddConfiguration(new SadPalletItemMap());

        modelBuilder.AddConfiguration(new TaxInspectionMap());

        modelBuilder.AddConfiguration(new ProductCapitalizationMap());

        modelBuilder.AddConfiguration(new ProductCapitalizationItemMap());

        modelBuilder.AddConfiguration(new MergedServiceMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkrainePaymentDeliveryProtocolKeyMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkrainePaymentDeliveryProtocolMap());

        modelBuilder.AddConfiguration(new ExpiredBillUserNotificationMap());

        modelBuilder.AddConfiguration(new AdvancePaymentMap());

        modelBuilder.AddConfiguration(new ProductSlugMap());

        modelBuilder.AddConfiguration(new CarBrandMap());

        modelBuilder.AddConfiguration(new ProductCarBrandMap());

        modelBuilder.AddConfiguration(new OrderProductSpecificationMap());

        modelBuilder.AddConfiguration(new SupportVideoMap());

        modelBuilder.AddConfiguration(new AccountingDocumentNameMap());

        modelBuilder.AddConfiguration(new SaleReturnItemStatusNameMap());

        modelBuilder.AddConfiguration(new ConsignmentMap());

        modelBuilder.AddConfiguration(new ConsignmentItemMap());

        modelBuilder.AddConfiguration(new ConsignmentItemMovementMap());

        modelBuilder.AddConfiguration(new ConsignmentItemMovementTypeNameMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineCartItemReservationMap());

        modelBuilder.AddConfiguration(new TaxAccountingSchemeMap());

        modelBuilder.AddConfiguration(new AgreementTypeCivilCodeMap());

        modelBuilder.AddConfiguration(new CountSaleMessageMap());

        modelBuilder.AddConfiguration(new SaleMessageNumeratorMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineCartItemReservationProductPlacementMap());

        modelBuilder.AddConfiguration(new DataSyncOperationMap());

        modelBuilder.AddConfiguration(new VehicleServiceMap());

        modelBuilder.AddConfiguration(new SupplyOrderVehicleServiceMap());

        modelBuilder.AddConfiguration(new GovExchangeRateMap());

        modelBuilder.AddConfiguration(new GovExchangeRateHistoryMap());

        modelBuilder.AddConfiguration(new GovCrossExchangeRateMap());

        modelBuilder.AddConfiguration(new GovCrossExchangeRateHistoryMap());

        modelBuilder.AddConfiguration(new DeliveryProductProtocolMap());

        modelBuilder.AddConfiguration(new BillOfLadingServiceMap());

        modelBuilder.AddConfiguration(new SupplyInvoiceMergedServiceMap());

        modelBuilder.AddConfiguration(new SupplyInvoiceBillOfLadingServiceMap());

        modelBuilder.AddConfiguration(new DeliveryProductProtocolDocumentMap());

        modelBuilder.AddConfiguration(new DeliveryProductProtocolNumberMap());

        modelBuilder.AddConfiguration(new ReSaleItemMap());

        modelBuilder.AddConfiguration(new ReSaleMap());

        modelBuilder.AddConfiguration(new SupplyInvoiceDeliveryDocumentMap());

        modelBuilder.AddConfiguration(new SupplyInformationTaskMap());

        modelBuilder.AddConfiguration(new PackingListPackageOrderItemSupplyServiceMap());

        modelBuilder.AddConfiguration(new ActProvidingServiceDocumentMap());

        modelBuilder.AddConfiguration(new SupplyServiceAccountDocumentMap());

        modelBuilder.AddConfiguration(new ActProvidingServiceMap());

        modelBuilder.AddConfiguration(new SupplyOrderUkraineDocumentMap());

        modelBuilder.AddConfiguration(new ConsignmentNoteSettingMap());

        modelBuilder.AddConfiguration(new VatRateMap());

        modelBuilder.AddConfiguration(new ReSaleAvailabilityMap());

        modelBuilder.AddConfiguration(new EcommercePageMap());

        modelBuilder.AddConfiguration(new EcommerceContactsMap());

        modelBuilder.AddConfiguration(new EcommerceContactInfoMap());

        modelBuilder.AddConfiguration(new SeoPageMap());

        modelBuilder.AddConfiguration(new EcommerceDefaultPricingMap());

        modelBuilder.AddConfiguration(new RetailPaymentMap());

        modelBuilder.AddConfiguration(new RetailClientMap());

        modelBuilder.AddConfiguration(new BankMap());

        modelBuilder.AddConfiguration(new EcommerceRegionMap());

        modelBuilder.AddConfiguration(new MisplacedSaleMap());

        modelBuilder.AddConfiguration(new RetailClientPaymentImageMap());

        modelBuilder.AddConfiguration(new RetailClientPaymentImageItemMap());

        modelBuilder.AddConfiguration(new ClientGroupMap());

        modelBuilder.AddConfiguration(new ClientWorkplaceMap());

        modelBuilder.AddConfiguration(new WorkplaceMap());

        modelBuilder.AddConfiguration(new WorkplaceClientAgreementMap());

        modelBuilder.AddConfiguration(new RetailPaymentStatusMap());

        modelBuilder.AddConfiguration(new AccountingOperationNameMap());

        modelBuilder.AddConfiguration(new UserRoleDashboardNodeMap());

        modelBuilder.AddConfiguration(new PermissionMap());

        modelBuilder.AddConfiguration(new RolePermissionMap());

        modelBuilder.AddConfiguration(new DeliveryExpenseMap());

        modelBuilder.AddConfiguration(new CustomersOwnTtnMap());
    }
}