namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ProductIncomesSegments {
    public const string GET_ALL = "all";

    public const string GET_BY_NET_ID = "get";

    public const string GET_BY_SUPPLY_ORDER_NET_ID = "get/supply/order";

    public const string GET_BY_DELIVERY_PRODUCT_PROTOCOL_NET_ID = "get/delivery/product/protocol";

    public const string GET_ALL_BY_SUPPLY_UKRAINE_ORDER_NET_ID = "all/supply/ukraine";

    public const string GET_ALL_BY_PRODUCT_NET_ID = "all/product";

    public const string ADD_NEW_FROM_PACKING_LIST = "new/packinglist";

    public const string ADD_NEW_FROM_PACKING_LIST_DYNAMIC_PLACEMENTS = "new/packinglist/dynamic";

    public const string ADD_NEW_FROM_SUPPLY_ORDER_UKRAINE = "new/supply/ukraine";

    public const string ADD_NEW_FROM_SUPPLY_ORDER_UKRAINE_DYNAMIC_PLACEMENTS = "new/supply/ukraine/dynamic";

    public const string ADD_NEW_FROM_ACT_RECONCILIATION_ITEM = "new/reconciliation";

    public const string ADD_NEW_FROM_ACT_RECONCILIATION_ITEMS = "new/reconciliation/many";

    public const string EXPORT_PRINTING_DOCUMENT = "document/export";

    public const string SUPPLY_ORDER_UKRAINE_PRODUCT_INCOME_GET = "supply/order/ukraine/get";

    public const string SUPPLY_ORDER_PRODUCT_INCOME_GET = "supply/order/get";
}