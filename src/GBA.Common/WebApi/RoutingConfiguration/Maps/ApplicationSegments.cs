namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ApplicationSegments {
    public const string UserManagement = "usermanagement";

    public const string UserProfilesManagement = "usermanagement/profiles";

    public const string UserProfileRolesManagement = "usermanagement/profiles/roles";

    public const string UserProfileRoleTranslationsManagement = "usermanagement/profiles/roles/translations";

    public const string UserPermissions = "permissions";

    public const string UserNotes = "usermanagement/notes";

    public const string Agreements = "agreements";

    public const string AgreementTypes = "agreements/types";

    public const string AgreementTypesTranslations = "agreements/types/translations";

    public const string CalculationTypes = "agreements/calculationtypes";

    public const string CalculationTypesTranslations = "agreements/calculationtypes/translations";

    public const string Clients = "clients";

    public const string HistoryOrderItem = "history/order/item";

    public const string Incoterms = "incoterms";

    public const string ClientOrganizations = "clients/organizations";

    public const string ClientTypes = "clients/types";

    public const string ClientTypeRoles = "clients/types/roles";

    public const string ClientContractDocuments = "clients/documents";

    public const string ClientTypeRolesTranslations = "clients/types/roles/translations";

    public const string ClientTypesTranslations = "clients/types/translations";

    public const string PerfectClients = "clients/perfect";

    public const string PerfectClientsTranslations = "clients/perfect/translations";

    public const string PerfectClientValuesTranslations = "clients/perfect/values/translations";

    public const string ClientShoppingCartItems = "clients/shoppingcart/items";

    public const string Regions = "regions";

    public const string SeoData = "seo";

    public const string RegionCodes = "regions/codes";

    public const string Currencies = "currencies";

    public const string CurrencyTraders = "currencies/traders";

    public const string FilterItems = "filteritems";

    public const string Organizations = "organizations";

    public const string TaxInspections = "tax/inspections";

    public const string Vats = "vats";

    public const string Recommendations = "recommendations";

    public const string OrganizationTranslations = "organizations/translations";

    public const string Pricings = "pricings";

    public const string ProviderPricings = "pricings/provider";

    public const string PricingTranslations = "pricings/translations";

    public const string PricingTypes = "pricings/types";

    public const string PricingTypesTranslations = "pricings/types/translations";

    public const string MeasureUnits = "measureunits";

    public const string MeasureUnitsTranslations = "measureunits/translations";

    public const string Products = "products";

    public const string ProductSpecifications = "specifications";

    public const string CarBrands = "car/brands";

    public const string ProductCapitalizations = "products/capitalizations";

    public const string ProductPlacementMovements = "products/placements/movements";

    public const string ProductPlacementHistory = "products/placements/history";

    public const string ProductPlacementStorage = "products/placements/storage";

    public const string ProductWriteOffRules = "products/writeoff/rules";

    public const string ProductIncomes = "products/incomes";

    public const string ProductTransfers = "products/transfers";

    public const string ProductGroups = "products/groups";

    public const string ProductReservations = "products/reservations";

    public const string Categories = "categories";

    public const string SearchStrategy = "search";

    public const string OriginalNumbers = "originalnumbers";

    public const string ColumnItems = "columns";

    public const string Auditing = "auditing";

    public const string Transporters = "transporters";

    public const string TransporterTypes = "transporters/types";

    public const string TransporterTypeTranslations = "transporters/types/translations";

    public const string Storages = "storages";

    public const string Debts = "debts";

    public const string ExchangeRates = "exchangerates";

    public const string GovExchangeRates = "exchangerates/gov";

    public const string ExchangeRateCharts = "charts/exchangerates";

    public const string CurrencyTraderExchangeRateCharts = "charts/currencytraders/exchangerates";

    public const string GeoLocations = "geolocations";

    public const string CrossExchangeRates = "exchangerates/cross";

    public const string GovCrossExchangeRates = "exchangerates/cross/gov";

    public const string Sales = "sales";

    public const string ProtocolActInvoice = "protocol/act/invoice";

    public const string ShipmentLists = "sales/shipments";

    public const string SaleReturns = "sales/returns";

    public const string SaleOffers = "sales/offers";

    public const string SaleReservations = "sales/reservations";

    public const string SalePrediction = "sales/prediction";

    public const string Orders = "orders";

    public const string PreOrders = "preorders";

    public const string OrderItems = "orders/items";

    public const string DeliveryRecipients = "deliveries/recipients";

    public const string DeliveryRecipientAddresses = "deliveries/recipients/addresses";

    public const string TermsOfDeliveries = "deliveries/terms";

    public const string SchedulerTasks = "tasks/scheduler";

    public const string SupplyOrders = "supplies/orders";

    public const string SupplyReturns = "supplies/returns";

    public const string SupplyOrganizations = "supplies/organizations";

    public const string SupplyOrderItems = "supplies/orders/items";

    public const string SupplyProForms = "supplies/proforms";

    public const string SupplyInvoices = "supplies/invoices";

    public const string SupplyDeliveryDocuments = "supplies/documents";

    public const string PackingLists = "supplies/packinglists";

    public const string TransportationServices = "supplies/services/transportations";

    public const string PortWorkServices = "supplies/services/portworks";

    public const string ContainerServices = "supplies/services/containers";

    public const string VehicleServices = "supplies/services/vehicles";

    public const string CustomServices = "supplies/services/customs";

    public const string MergedServices = "supplies/services/merged";

    public const string PortCustomAgencyServices = "supplies/services/customs/portagency";

    public const string VehicleDeliveryServices = "supplies/services/vehicle";

    public const string CustomAgencyServices = "supplies/services/customs/agency";

    public const string PlaneDeliveryServices = "supplies/services/plane";

    public const string ServiceDetailItems = "supplies/services/items";

    public const string SupplyServicesSearch = "supplies/services/search";

    public const string Countries = "countries";

    public const string PackingMarkings = "packings";

    public const string Dashboards = "dashboards";

    public const string DataSync = "data/sync";

    public const string PaymentRegisters = "payments/registers";

    public const string PaymentRegisterTransfers = "payments/registers/transfers";

    public const string PaymentRegisterCurrencyExchanges = "payments/registers/exchanges";

    public const string PaymentMovements = "payments/movements";

    public const string PaymentCostMovements = "payments/costs/movements";

    public const string AdvancePayments = "payments/advance";

    public const string IncomePaymentOrders = "payments/orders/income";

    public const string OutcomePaymentOrders = "payments/orders/outcome";

    public const string SupplyPaymentTasks = "payments/tasks";

    public const string GroupedPaymentTasks = "payments/tasks/grouped";

    public const string AccountingCashFlow = "accounting/cashflow";

    public const string AccountingPayableInfo = "accounting/payable/info";

    public const string ConsumableProducts = "consumables/products";

    public const string CompanyCars = "consumables/company/cars";

    public const string CompanyCarRoadLists = "consumables/company/cars/roadlists";

    public const string ConsumableProductCategories = "consumables/categories";

    public const string ConsumableOrders = "consumables/orders";

    public const string DepreciatedConsumableOrder = "consumables/orders/depreciated";

    public const string ConsumableStorages = "consumables/storages";

    public const string CarrierStatham = "supplies/ukraine/carriers/statham";

    public const string SupplyOrdersUkraine = "supplies/ukraine/order";

    public const string SupplyOrderUkraineCartItems = "supplies/ukraine/order/cart/items";

    public const string TaxFreePackLists = "supplies/ukraine/order/packlists/taxfree";

    public const string Sads = "supplies/ukraine/order/packlists/sad";

    public const string SadPalletTypes = "supplies/ukraine/order/packlists/sad/pallet/types";

    public const string TaxFrees = "supplies/ukraine/order/taxfree";

    public const string DynamicProductPlacementRows = "supplies/ukraine/order/placements/dynamic/rows";

    public const string ActReconciliation = "supplies/ukraine/reconciliation";

    public const string Debtors = "debtors";

    public const string DepreciatedOrders = "orders/depreciated";

    public const string ExpiredBillUserNotifications = "user/notifications/expiredbill";

    public const string SupportVideos = "supports/videos";

    public const string XmlDocument = "xml/documents";

    public const string RemainingConsignments = "consignments/remaining";

    public const string ConsignmentsInfo = "consignments/info";

    public const string DeliveryProductProtocol = "delivery/product/protocol";

    public const string BillOfLadingServices = "bill/lading/services";

    public const string Dashboard = "totals/dashboard";

    public const string ReSales = "resales";

    public const string ActProvidingServices = "act/providing/services";

    public const string XlsDocument = "xls/documents";

    public const string ConsignmentNoteSettings = "consignment/note/settings";

    public const string VatRates = "vat/rates";

    public const string SeoInfo = "seo/info";

    public const string RetailClients = "retail/clients";

    public const string Banks = "bank";

    public const string Operation = "operation";

    public const string DocumentsAfterSync = "documents/sync";

    public const string GBAData = "gba/data";
}