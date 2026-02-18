using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PolishUserDetails;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.Entities.UserNotifications;

namespace GBA.Domain.Entities;

public sealed class User : EntityBase {
    /// <summary>
    /// ctor().
    /// </summary>
    public User() {
        ClientUserProfiles = new HashSet<ClientUserProfile>();

        Sales = new HashSet<Sale>();

        SalesChangedToInvoice = new HashSet<Sale>();

        Orders = new HashSet<Order>();

        OrderItems = new HashSet<OrderItem>();

        OrderItemBaseShiftStatuses = new HashSet<OrderItemBaseShiftStatus>();

        UserScreenResolutions = new HashSet<UserScreenResolution>();

        ResponsibilityDeliveryProtocols = new HashSet<ResponsibilityDeliveryProtocol>();

        SupplyOrderPaymentDeliveryProtocols = new HashSet<SupplyOrderPaymentDeliveryProtocol>();

        SupplyInformationDeliveryProtocols = new HashSet<SupplyInformationDeliveryProtocol>();

        SupplyPaymentTasks = new HashSet<SupplyPaymentTask>();

        SupplyOrderDeliveryDocuments = new HashSet<SupplyOrderDeliveryDocument>();

        ContainerServices = new HashSet<ContainerService>();

        BrokerServices = new HashSet<CustomService>();

        PortWorkServices = new HashSet<PortWorkService>();

        TransportationServices = new HashSet<TransportationService>();

        PortCustomAgencyServices = new HashSet<PortCustomAgencyService>();

        CustomAgencyServices = new HashSet<CustomAgencyService>();

        PlaneDeliveryServices = new HashSet<PlaneDeliveryService>();

        VehicleDeliveryServices = new HashSet<VehicleDeliveryService>();

        SupplyOrderPolandPaymentDeliveryProtocols = new HashSet<SupplyOrderPolandPaymentDeliveryProtocol>();

        ProductSpecifications = new HashSet<ProductSpecification>();

        OrderPackageUsers = new HashSet<OrderPackageUser>();

        PaymentRegisterTransfers = new HashSet<PaymentRegisterTransfer>();

        PaymentRegisterCurrencyExchanges = new HashSet<PaymentRegisterCurrencyExchange>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        ColleagueOutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        ColleagueIncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        ConsumablesOrders = new HashSet<ConsumablesOrder>();

        ConsumablesStorages = new HashSet<ConsumablesStorage>();

        CreatedDepreciatedConsumableOrders = new HashSet<DepreciatedConsumableOrder>();

        UpdatedDepreciatedConsumableOrders = new HashSet<DepreciatedConsumableOrder>();

        DepreciatedConsumableOrders = new HashSet<DepreciatedConsumableOrder>();

        HeadOfDepreciatedConsumableOrders = new HashSet<DepreciatedConsumableOrder>();

        CreatedCompanyCars = new HashSet<CompanyCar>();

        UpdatedCompanyCars = new HashSet<CompanyCar>();

        CompanyCarRoadLists = new HashSet<CompanyCarRoadList>();

        CreatedCompanyCarRoadLists = new HashSet<CompanyCarRoadList>();

        UpdatedCompanyCarRoadLists = new HashSet<CompanyCarRoadList>();

        CompanyCarFuelings = new HashSet<CompanyCarFueling>();

        CompanyCarRoadListDrivers = new HashSet<CompanyCarRoadListDriver>();

        ExchangeRateHistories = new HashSet<ExchangeRateHistory>();

        CrossExchangeRateHistories = new HashSet<CrossExchangeRateHistory>();

        OffersProcessingStatusChanged = new HashSet<ClientShoppingCart>();

        OfferItemsProcessingStatusChanged = new HashSet<OrderItem>();

        UpdatedOrderItemOneTimeDiscounts = new HashSet<OrderItem>();

        ClientShoppingCarts = new HashSet<ClientShoppingCart>();

        OrderItemMovements = new HashSet<OrderItemMovement>();

        CreatedSaleReturnItems = new HashSet<SaleReturnItem>();

        UpdatedSaleReturnItems = new HashSet<SaleReturnItem>();

        MoneyReturnedSaleReturnItems = new HashSet<SaleReturnItem>();

        CreatedSaleReturns = new HashSet<SaleReturn>();

        UpdatedSaleReturns = new HashSet<SaleReturn>();

        CanceledSaleReturns = new HashSet<SaleReturn>();

        ProductIncomes = new HashSet<ProductIncome>();

        CreatedProductWriteOffRules = new HashSet<ProductWriteOffRule>();

        UpdatedProductWriteOffRules = new HashSet<ProductWriteOffRule>();

        ResponsibleSupplyOrderUkraines = new HashSet<SupplyOrderUkraine>();

        ActReconciliations = new HashSet<ActReconciliation>();

        ResponsibleDepreciatedOrders = new HashSet<DepreciatedOrder>();

        ResponsibleProductTransfers = new HashSet<ProductTransfer>();

        SupplyReturns = new HashSet<SupplyReturn>();

        ResponsibleProductPlacementMovements = new HashSet<ProductPlacementMovement>();

        ResponsibleShipmentLists = new HashSet<ShipmentList>();

        ResponsibleTaxFreePackLists = new HashSet<TaxFreePackList>();

        ResponsibleTaxFrees = new HashSet<TaxFree>();

        ResponsibleSads = new HashSet<Sad>();

        UpdatedSupplyPaymentTasks = new HashSet<SupplyPaymentTask>();

        DeletedSupplyPaymentTasks = new HashSet<SupplyPaymentTask>();

        ProductCapitalizations = new HashSet<ProductCapitalization>();

        MergedServices = new HashSet<MergedService>();

        SupplyOrderUkrainePaymentDeliveryProtocols = new HashSet<SupplyOrderUkrainePaymentDeliveryProtocol>();

        ManagerExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        CreatedByExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        LockedByExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        LastViewedByExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        ProcessedByExpiredBillUserNotifications = new HashSet<ExpiredBillUserNotification>();

        AdvancePayments = new HashSet<AdvancePayment>();

        DataSyncOperations = new HashSet<DataSyncOperation>();

        VehicleServices = new HashSet<VehicleService>();

        GovExchangeRateHistories = new HashSet<GovExchangeRateHistory>();

        GovCrossExchangeRateHistories = new HashSet<GovCrossExchangeRateHistory>();

        BillOfLadingServices = new HashSet<BillOfLadingService>();

        DeliveryProductProtocols = new HashSet<DeliveryProductProtocol>();

        ReSales = new HashSet<ReSale>();

        SupplyInformationTasks = new HashSet<SupplyInformationTask>();

        UpdatedSupplyInformationTasks = new HashSet<SupplyInformationTask>();

        DeletedSupplyInformationTasks = new HashSet<SupplyInformationTask>();

        ActProvidingServices = new HashSet<ActProvidingService>();

        MisplacedSales = new HashSet<MisplacedSale>();

        RetailClientPaymentImageItems = new HashSet<RetailClientPaymentImageItem>();

        Clients = new HashSet<Client>();

        DeliveryExpenses = new HashSet<DeliveryExpense>();
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public string Abbreviation { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string Region { get; set; }

    public long? UserRoleId { get; set; }

    public long? UserDetailsId { get; set; }

    public bool IsActive { get; set; }

    public UserRole UserRole { get; set; }

    public UserDetails UserDetails { get; set; }

    public ICollection<ClientUserProfile> ClientUserProfiles { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<Sale> SalesChangedToInvoice { get; set; }

    public ICollection<RetailClientPaymentImageItem> RetailClientPaymentImageItems { get; set; }

    public ICollection<Order> Orders { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }

    public ICollection<OrderItemBaseShiftStatus> OrderItemBaseShiftStatuses { get; set; }

    public ICollection<UserScreenResolution> UserScreenResolutions { get; set; }

    public ICollection<ResponsibilityDeliveryProtocol> ResponsibilityDeliveryProtocols { get; set; }

    public ICollection<SupplyOrderPaymentDeliveryProtocol> SupplyOrderPaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyInformationDeliveryProtocol> SupplyInformationDeliveryProtocols { get; set; }

    public ICollection<SupplyPaymentTask> SupplyPaymentTasks { get; set; }

    public ICollection<SupplyOrderDeliveryDocument> SupplyOrderDeliveryDocuments { get; set; }

    public ICollection<ContainerService> ContainerServices { get; set; }

    public ICollection<VehicleService> VehicleServices { get; set; }

    public ICollection<CustomService> BrokerServices { get; set; }

    public ICollection<PortWorkService> PortWorkServices { get; set; }

    public ICollection<TransportationService> TransportationServices { get; set; }

    public ICollection<PortCustomAgencyService> PortCustomAgencyServices { get; set; }

    public ICollection<CustomAgencyService> CustomAgencyServices { get; set; }

    public ICollection<PlaneDeliveryService> PlaneDeliveryServices { get; set; }

    public ICollection<VehicleDeliveryService> VehicleDeliveryServices { get; set; }

    public ICollection<SupplyOrderPolandPaymentDeliveryProtocol> SupplyOrderPolandPaymentDeliveryProtocols { get; set; }

    public ICollection<ProductSpecification> ProductSpecifications { get; set; }

    public ICollection<OrderPackageUser> OrderPackageUsers { get; set; }

    public ICollection<PaymentRegisterTransfer> PaymentRegisterTransfers { get; set; }

    public ICollection<PaymentRegisterCurrencyExchange> PaymentRegisterCurrencyExchanges { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> ColleagueOutcomePaymentOrders { get; set; }

    public ICollection<IncomePaymentOrder> ColleagueIncomePaymentOrders { get; set; }

    public ICollection<ConsumablesOrder> ConsumablesOrders { get; set; }

    public ICollection<ConsumablesStorage> ConsumablesStorages { get; set; }

    public ICollection<DepreciatedConsumableOrder> CreatedDepreciatedConsumableOrders { get; set; }

    public ICollection<DepreciatedConsumableOrder> UpdatedDepreciatedConsumableOrders { get; set; }

    public ICollection<DepreciatedConsumableOrder> DepreciatedConsumableOrders { get; set; }

    public ICollection<DepreciatedConsumableOrder> HeadOfDepreciatedConsumableOrders { get; set; }

    public ICollection<CompanyCar> CreatedCompanyCars { get; set; }

    public ICollection<CompanyCar> UpdatedCompanyCars { get; set; }

    public ICollection<CompanyCarRoadList> CompanyCarRoadLists { get; set; }

    public ICollection<CompanyCarRoadList> CreatedCompanyCarRoadLists { get; set; }

    public ICollection<CompanyCarRoadList> UpdatedCompanyCarRoadLists { get; set; }

    public ICollection<CompanyCarFueling> CompanyCarFuelings { get; set; }

    public ICollection<CompanyCarRoadListDriver> CompanyCarRoadListDrivers { get; set; }

    public ICollection<ExchangeRateHistory> ExchangeRateHistories { get; set; }

    public ICollection<CrossExchangeRateHistory> CrossExchangeRateHistories { get; set; }

    public ICollection<ClientShoppingCart> OffersProcessingStatusChanged { get; set; }

    public ICollection<OrderItem> OfferItemsProcessingStatusChanged { get; set; }

    public ICollection<OrderItem> UpdatedOrderItemOneTimeDiscounts { get; set; }

    public ICollection<ClientShoppingCart> ClientShoppingCarts { get; set; }

    public ICollection<OrderItemMovement> OrderItemMovements { get; set; }

    public ICollection<SaleReturnItem> CreatedSaleReturnItems { get; set; }

    public ICollection<SaleReturnItem> UpdatedSaleReturnItems { get; set; }

    public ICollection<SaleReturnItem> MoneyReturnedSaleReturnItems { get; set; }

    public ICollection<SaleReturn> CreatedSaleReturns { get; set; }

    public ICollection<SaleReturn> UpdatedSaleReturns { get; set; }

    public ICollection<SaleReturn> CanceledSaleReturns { get; set; }

    public ICollection<ProductIncome> ProductIncomes { get; set; }

    public ICollection<ProductWriteOffRule> CreatedProductWriteOffRules { get; set; }

    public ICollection<ProductWriteOffRule> UpdatedProductWriteOffRules { get; set; }

    public ICollection<SupplyOrderUkraine> ResponsibleSupplyOrderUkraines { get; set; }

    public ICollection<ActReconciliation> ActReconciliations { get; set; }

    public ICollection<DepreciatedOrder> ResponsibleDepreciatedOrders { get; set; }

    public ICollection<ProductTransfer> ResponsibleProductTransfers { get; set; }

    public ICollection<SupplyReturn> SupplyReturns { get; set; }

    public ICollection<ProductPlacementMovement> ResponsibleProductPlacementMovements { get; set; }

    public ICollection<ShipmentList> ResponsibleShipmentLists { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> CreatedSupplyOrderUkraineCartItems { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> UpdatedSupplyOrderUkraineCartItems { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> ResponsibleSupplyOrderUkraineCartItems { get; set; }

    public ICollection<TaxFreePackList> ResponsibleTaxFreePackLists { get; set; }

    public ICollection<TaxFree> ResponsibleTaxFrees { get; set; }

    public ICollection<Sad> ResponsibleSads { get; set; }

    public ICollection<SupplyPaymentTask> UpdatedSupplyPaymentTasks { get; set; }

    public ICollection<SupplyPaymentTask> DeletedSupplyPaymentTasks { get; set; }

    public ICollection<SupplyInformationTask> SupplyInformationTasks { get; set; }

    public ICollection<SupplyInformationTask> UpdatedSupplyInformationTasks { get; set; }

    public ICollection<SupplyInformationTask> DeletedSupplyInformationTasks { get; set; }

    public ICollection<ProductCapitalization> ProductCapitalizations { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<SupplyOrderUkrainePaymentDeliveryProtocol> SupplyOrderUkrainePaymentDeliveryProtocols { get; set; }

    public ICollection<ExpiredBillUserNotification> ManagerExpiredBillUserNotifications { get; set; }

    public ICollection<ExpiredBillUserNotification> CreatedByExpiredBillUserNotifications { get; set; }

    public ICollection<ExpiredBillUserNotification> LockedByExpiredBillUserNotifications { get; set; }

    public ICollection<ExpiredBillUserNotification> LastViewedByExpiredBillUserNotifications { get; set; }

    public ICollection<ExpiredBillUserNotification> ProcessedByExpiredBillUserNotifications { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }

    public ICollection<DataSyncOperation> DataSyncOperations { get; set; }

    public ICollection<GovExchangeRateHistory> GovExchangeRateHistories { get; set; }

    public ICollection<GovCrossExchangeRateHistory> GovCrossExchangeRateHistories { get; set; }

    public ICollection<BillOfLadingService> BillOfLadingServices { get; set; }

    public ICollection<DeliveryProductProtocol> DeliveryProductProtocols { get; set; }

    public ICollection<ReSale> ReSales { get; set; }

    public ICollection<ActProvidingService> ActProvidingServices { get; set; }

    public ICollection<ReSale> ChangeToInvoiceReSales { get; set; }

    public ICollection<MisplacedSale> MisplacedSales { get; set; }

    public ICollection<Client> Clients { get; set; }

    public ICollection<DeliveryExpense> DeliveryExpenses { get; set; }
}