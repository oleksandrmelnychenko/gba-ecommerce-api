using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Accounting;
using GBA.Services.Actors.Agreements;
using GBA.Services.Actors.AllegroServices;
using GBA.Services.Actors.Auditing;
using GBA.Services.Actors.Banks;
using GBA.Services.Actors.Banks.BankGetActors;
using GBA.Services.Actors.Categories;
using GBA.Services.Actors.Categories.CategoriesGetActors;
using GBA.Services.Actors.Charts;
using GBA.Services.Actors.Clients;
using GBA.Services.Actors.ColumnItems;
using GBA.Services.Actors.Communications;
using GBA.Services.Actors.ConsignmentNoteSettings;
using GBA.Services.Actors.Consignments;
using GBA.Services.Actors.Consumables;
using GBA.Services.Actors.Countries;
using GBA.Services.Actors.CrossExchangeRates;
using GBA.Services.Actors.CrossExchangeRates.CrossExchangeRatesGetActors;
using GBA.Services.Actors.CrossExchangeRates.GovCrossExchangeRatesGetActors;
using GBA.Services.Actors.Currencies;
using GBA.Services.Actors.Dashboards;
using GBA.Services.Actors.Dashboards.DashboardsGetActors;
using GBA.Services.Actors.DataSync;
using GBA.Services.Actors.DataSync.BaseDataSyncGetActors;
using GBA.Services.Actors.Debtors;
using GBA.Services.Actors.Deliveries;
using GBA.Services.Actors.DepreciatedOrders;
using GBA.Services.Actors.Ecommerce;
using GBA.Services.Actors.ExchangeRates;
using GBA.Services.Actors.Filters;
using GBA.Services.Actors.GbaData;
using GBA.Services.Actors.Logging;
using GBA.Services.Actors.Measures;
using GBA.Services.Actors.Organizations;
using GBA.Services.Actors.OriginalNumbers;
using GBA.Services.Actors.PaymentOrders;
using GBA.Services.Actors.Pricings;
using GBA.Services.Actors.Products;
using GBA.Services.Actors.Regions;
using GBA.Services.Actors.ReSales;
using GBA.Services.Actors.SaleReturns;
using GBA.Services.Actors.Sales;
using GBA.Services.Actors.SchedulerTasks;
using GBA.Services.Actors.SearchStrategy;
using GBA.Services.Actors.Storages;
using GBA.Services.Actors.Supplies;
using GBA.Services.Actors.Supports;
using GBA.Services.Actors.TaxInspections;
using GBA.Services.Actors.TotalDashboards;
using GBA.Services.Actors.Translations;
using GBA.Services.Actors.Transporters;
using GBA.Services.Actors.UserManagement;
using GBA.Services.Actors.UserNotifications;
using GBA.Services.Actors.Vats;
using GBA.Services.Actors.XmlDocuments;

namespace GBA.Services.Actors;

public sealed class MasterActor : ReceiveActor {
    public MasterActor() {
        ActorReferenceManager.Instance.Add(
            BaseActorNames.USER_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<UserManagementActor>(), BaseActorNames.USER_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CONSIGNMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsignmentsActor>(), BaseActorNames.CONSIGNMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.COMMUNICATIONS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<CommunicationsManagementActor>(), BaseActorNames.COMMUNICATIONS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.AGREEMENTS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<AgreementsManagementActor>(), BaseActorNames.AGREEMENTS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.TRANSLATION_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<TranslationManagementActor>(), BaseActorNames.TRANSLATION_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.AUDIT_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<AuditManagementActor>(), BaseActorNames.AUDIT_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CLIENTS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientsManagementActor>(), BaseActorNames.CLIENTS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.PRICING_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<PricingManagementActor>(), BaseActorNames.PRICING_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.REGIONS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<RegionsManagementActor>(), BaseActorNames.REGIONS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductsManagementActor>(), BaseActorNames.PRODUCTS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.FILTERS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<FiltersManagementActor>(), BaseActorNames.FILTERS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CURRENCY_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<CurrencyManagementActor>(), BaseActorNames.CURRENCY_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.ORGANIZATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<OrganizationsActor>(), BaseActorNames.ORGANIZATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.MEASURE_UNITS_ACTOR,
            Context.ActorOf(Context.DI().Props<MeasureUnitsActor>(), BaseActorNames.MEASURE_UNITS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CATEGORIES_ACTOR,
            Context.ActorOf(Context.DI().Props<CategoriesActor>(), BaseActorNames.CATEGORIES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_CATEGORIES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseCategoriesGetActor>(), BaseActorNames.BASE_CATEGORIES_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SEARCH_STRATEGY_ACTOR,
            Context.ActorOf(Context.DI().Props<SearchStrategyActor>().WithRouter(new RoundRobinPool(10)), BaseActorNames.SEARCH_STRATEGY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.ORIGINAL_NUMBERS_ACTOR,
            Context.ActorOf(Context.DI().Props<OriginalNumbersActor>(), BaseActorNames.ORIGINAL_NUMBERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.COLUMN_ITEMS_ACTOR,
            Context.ActorOf(Context.DI().Props<ColumnItemsActor>(), BaseActorNames.COLUMN_ITEMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.TRANSPORTER_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<TransporterManagementActor>(), BaseActorNames.TRANSPORTER_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.STORAGES_ACTOR,
            Context.ActorOf(Context.DI().Props<StoragesActor>(), BaseActorNames.STORAGES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<ExchangeRatesActor>(), BaseActorNames.EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_GET_EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseGetExchangeRatesActor>().WithRouter(new RoundRobinPool(10)), BaseActorNames.BASE_GET_EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.GOV_EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<GovExchangeRatesActor>(), BaseActorNames.GOV_EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_GET_GOV_EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseGetGovExchangeRatesActor>().WithRouter(new RoundRobinPool(10)), BaseActorNames.BASE_GET_GOV_EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.DELIVERY_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<DeliveryManagementActor>(), BaseActorNames.DELIVERY_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SALES_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SalesManagementActor>(), BaseActorNames.SALES_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.PROTOCOL_ACT_INVOICE_ACTOR,
            Context.ActorOf(Context.DI().Props<ProtocolActEditInvoicetActor>(), BaseActorNames.PROTOCOL_ACT_INVOICE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SCHEDULER_TASKS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SchedulerTasksManagementActor>(), BaseActorNames.SCHEDULER_TASKS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SUPPLY_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyManagementActor>(), BaseActorNames.SUPPLY_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.COUNTRIES_ACTOR,
            Context.ActorOf(Context.DI().Props<CountriesActor>(), BaseActorNames.COUNTRIES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CROSS_EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<CrossExchangeRatesActor>(), BaseActorNames.CROSS_EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_CROSS_EXCHANGE_RATES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseCrossExchangeRatesGetActor>().WithRouter(new RoundRobinPool(10)),
                BaseActorNames.BASE_CROSS_EXCHANGE_RATES_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.GOV_CROSS_EXCHANGE_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<GovCrossExchangeRatesActor>(), BaseActorNames.GOV_CROSS_EXCHANGE_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_GOV_CROSS_EXCHANGE_RATES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseGovCrossExchangeRatesGetActor>().WithRouter(new RoundRobinPool(10)),
                BaseActorNames.BASE_GOV_CROSS_EXCHANGE_RATES_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.ALLEGRO_SERVICES_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroServicesManagementActor>(), BaseActorNames.ALLEGRO_SERVICES_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.DASHBOARDS_ACTOR,
            Context.ActorOf(Context.DI().Props<DashboardsActor>(), BaseActorNames.DASHBOARDS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_DASHBOARDS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseDashboardsGetActor>(), BaseActorNames.BASE_DASHBOARDS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.PAYMENT_ORDERS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentOrdersManagementActor>(), BaseActorNames.PAYMENT_ORDERS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CONSUMABLES_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsumablesManagementActor>(), BaseActorNames.CONSUMABLES_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.LOG_MANAGER_ACTOR,
            Context.ActorOf(Context.DI().Props<LogManagerActor>(), BaseActorNames.LOG_MANAGER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CHARTS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ChartsManagementActor>(), BaseActorNames.CHARTS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.ACCOUNTING_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<AccountingManagementActor>(), BaseActorNames.ACCOUNTING_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.DEBTORS_ACTOR,
            Context.ActorOf(Context.DI().Props<DebtorsActor>().WithRouter(new RoundRobinPool(5)), BaseActorNames.DEBTORS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SALE_RETURNS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SaleReturnsManagementActor>(), BaseActorNames.SALE_RETURNS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.DEPRECIATED_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<DepreciatedManagementActor>(), BaseActorNames.DEPRECIATED_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.VATS_ACTOR,
            Context.ActorOf(Context.DI().Props<VatsActor>(), BaseActorNames.VATS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.TAX_INSPECTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<TaxInspectionsActor>(), BaseActorNames.TAX_INSPECTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.USER_NOTIFICATIONS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<UserNotificationsManagementActor>(), BaseActorNames.USER_NOTIFICATIONS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.SUPPORT_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SupportManagementActor>(), BaseActorNames.SUPPORT_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.XML_DOCUMENT_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<XmlDocumentManagementActor>(), BaseActorNames.XML_DOCUMENT_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.REMAINING_CONSIGNMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<RemainingConsignmentsActor>().WithRouter(new RoundRobinPool(10)), BaseActorNames.REMAINING_CONSIGNMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CONSIGNMENTS_INFO_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsignmentsInfoActor>().WithRouter(new RoundRobinPool(10)), BaseActorNames.CONSIGNMENTS_INFO_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<DataSyncManagementActor>(), BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.GBA_DATA_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<GbaDataManagementActor>(), BaseActorNames.GBA_DATA_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.TOTAL_DASHBOARDS_ACTOR,
            Context.ActorOf(Context.DI().Props<TotalDashboardsActor>(), BaseActorNames.TOTAL_DASHBOARDS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.RE_SALE_ACTOR,
            Context.ActorOf(Context.DI().Props<ReSaleActor>(), BaseActorNames.RE_SALE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.CONSIGNMENT_NOTE_SETTINGS_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsignmentNoteSettingsManagementActor>(),
                BaseActorNames.CONSIGNMENT_NOTE_SETTINGS_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.VAT_RATES_ACTOR,
            Context.ActorOf(Context.DI().Props<VatRatesActor>(), BaseActorNames.VAT_RATES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.RESALE_AVAILABILITY_ACTOR,
            Context.ActorOf(Context.DI().Props<ReSaleAvailabilityActor>(),
                BaseActorNames.RESALE_AVAILABILITY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.ECOMMERCE_SEO_ACTOR,
            Context.ActorOf(Context.DI().Props<EcommerceAdminPanelActor>(),
                BaseActorNames.ECOMMERCE_SEO_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BANK_ACTOR,
            Context.ActorOf(Context.DI().Props<BankActor>(),
                BaseActorNames.BANK_ACTOR));

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_BANK_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseBankGetActor>(),
                BaseActorNames.BASE_BANK_GET_ACTOR));

        ActorReferenceManager.Instance.Add(
            SalesActorNames.MISPLACED_SALE_ACTOR,
            Context.ActorOf(Context.DI().Props<MisplacedSaleActor>(),
                SalesActorNames.MISPLACED_SALE_ACTOR));

        ActorReferenceManager.Instance.Add(
            BaseActorNames.WORKPLACE_ACTOR,
            Context.ActorOf(Context.DI().Props<WorkplaceActor>(),
                BaseActorNames.WORKPLACE_ACTOR));

        // ActorReferenceManager.Instance.Add(
        //     BaseActorNames.PAYMENT_TYPE_OF_OPERATION_ACTOR,
        //     Context.ActorOf(Context.DI().Props<PaymentTypeOfOperationActor>(),
        //         BaseActorNames.PAYMENT_TYPE_OF_OPERATION_ACTOR)
        // );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.BASE_DOCUMENTS_AFTER_SYNC_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseDocumentsAfterSyncGetActor>(),
                BaseActorNames.BASE_DOCUMENTS_AFTER_SYNC_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            BaseActorNames.PRODUCT_PLACEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductPlacementActor>(),
                BaseActorNames.PRODUCT_PLACEMENT_ACTOR)
        );
    }
}