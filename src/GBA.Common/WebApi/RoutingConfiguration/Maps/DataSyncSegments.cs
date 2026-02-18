namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class DataSyncSegments {
    public const string START_DATA_SYNCHRONIZATION = "start";

    public const string START_INCOMED_ORDERS_SYNCHRONIZATION = "start/orders/incomed";

    public const string START_OUTCOME_ORDERS_SYNCHRONIZATION = "start/orders/outcome";

    public const string START_DAILY_SYNC = "start/daily";

    public const string GET_DOCUMENTS_AFTER_SYNCHRONIZATION = "get";

    public const string LAST_SYNC_INFO_GET = "info/get";
}