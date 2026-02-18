namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class OrdersSegments {
    public const string ADD_NEW = "new";

    public const string ADD_NEW_FROM_OFFER = "new/offer";

    public const string ADD_NEW_AS_INVOICE = "new/invoice";

    public const string ADD_NEW_AS_QUICK_INVOICE = "new/quick/invoice";

    public const string CALCULATE_TOTAL_PRICES = "calculate";

    public const string CALCULATE_TOTAL_PRICES_FOR_CHANGED_OFFER = "calculate/offer";

    public const string GET_ALL_ORDERS_FROM_SHOP = "all/shop";

    public const string GET_ALL_ORDERS_FROM_SHOP_BY_USER_NET_ID = "all/shop/user";

    public const string GET_ALL_ORDERS_FROM_SHOP_BY_CLIENT_NET_ID = "all/shop/client";

    public const string GET_ALL_SHOP_ORDERS_TOTAL_AMOUNT = "all/shop/total";

    public const string GET_ALL_SHOP_ORDERS_TOTAL_AMOUNT_BY_USER_NET_ID = "all/shop/total/user";

    public const string GET_ECOMMERCE_OFFER_BY_NET_ID = "offer/get";

    public const string GET_ALL_AVAILABLE_FOR_CLIENT_ECOMMERCE_OFFERS = "offer/all/available";

    public const string UPLOAD_CLIENT_PAYMENT_CONFIRMATION = "payment/upload";
}