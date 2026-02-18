namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class SchedulerTasksSegments {
    public const string MERGE_ALL_SALES = "sales/merge/all";

    public const string CLEAR_INVALID_SHOPPING_CARTS = "clients/shoppingcart/clear";

    public const string UPDATE_PRODUCT_PRICES = "products/prices/update";

    public const string UPDATE_PRODUCTS_AVAILABILITY_PL = "products/availability/update/pl";

    public const string UPDATE_PRODUCTS_AVAILABILITY_UA = "products/availability/update/ua";

    public const string GENERATE_EXPIRED_BILL_NOTIFICATIONS = "sales/expired/notification/generate";

    public const string DEFER_EXPIRED_BILL_NOTIFICATIONS = "sales/expired/notification/defer";
}