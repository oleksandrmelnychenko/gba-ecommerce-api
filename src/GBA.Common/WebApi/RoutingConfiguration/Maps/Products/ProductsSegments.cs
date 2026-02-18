namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ProductsSegments {
    public const string ADD_NEW = "new";

    public const string ADD_NEW_WITH_UPLOADS = "new/upload";

    public const string ADD_NEW_SPECIFICATION = "specification/new";

    public const string ADD_NEW_SPECIFICATION_FOR_ALL_PRODUCTS = "specification/new/all";

    public const string ADD_NEW_SPECIFICATION_FOR_ALL_PRODUCTS_FROM_FILE = "specification/new/all/file";

    public const string ADD_NEW_SPECIFICATION_FOR_CURRENT_PRODUCT = "specification/new/current";

    public const string GET_SPECIFICATIONS_FROM_SEARCH = "specification/search";

    public const string GET_ALL = "all";

    public const string GET_ALL_LIMITED = "all/limited";

    public const string GET_ALL_LIMITED_BY_GROUP_NET_ID = "all/limited/by/group";

    public const string GET_BY_NET_ID = "get";

    public const string GET_BY_SLUG = "get/slug";

    public const string GET_LAST_PRODUCT_PLACEMENT = "placements/get/last";

    public const string GET_PRODUCT_INCOME_INFO_BY_NET_ID = "income/info/get";

    public const string GET_ANALOGUES_BY_PRODUCT_NET_ID = "get/analogues";

    public const string GET_COMPONENTS_BY_PRODUCT_NET_ID = "get/components";

    public const string GET_PRICINGS_BY_PRODUCT = "pricings/all";

    public const string GET_CURRENT_PRICING_BY_PRODUCT = "pricings/current";

    public const string GET_TOP_TOTAL_PURCHASED_BY_ONLINE_SHOP = "get/top/purchased/shop";

    public const string GET_AVAILABILITIES_BY_PRODUCT_NET_ID = "all/availabilities/product";

    public const string GET_ORDERED_PRODUCTS_HISTORY = "get/ordered/products/history";

    public const string UPDATE = "update";

    public const string UPDATE_WITH_UPLOADS = "update/upload";

    public const string DELETE = "delete";

    public const string SEARCH = "search";

    public const string SEARCH_V2 = "search/v2";

    public const string SEARCH_V3 = "search/v3";

    public const string SEARCH_V3_DEBUG = "search/v3/debug";

    public const string ADVANCED_SEARCH = "search/advanced";

    public const string SEARCH_SIMILAR = "search/similar";

    public const string SEARCH_BY_VENDOR_CODE = "search/vendorcode";

    public const string SEARCH_BY_VENDOR_CODE_AND_SALES = "search/vendorcodeandsales";

    public const string SEARCH_BY_SPECIFICATION = "search/specification";

    public const string GET_ALL_BY_VENDOR_CODES = "vendorcodes/all";

    public const string ECOMMERCE_SYNC_AVAILABILITY = "sync/availability";

    public const string GET_ALL_ORDERED_PRODUCTS = "all/ordered";

    public const string UPLOAD_PRODUCTS_FROM_FILE = "upload/file";

    public const string UPLOAD_ANALOGUE_PRODUCTS_FROM_FILE = "upload/analogues/file";

    public const string UPLOAD_COMPONENTS_PRODUCTS_FROM_FILE = "upload/components/file";

    public const string UPLOAD_ORIGINAL_NUMBERS_FROM_FILE = "upload/oems/file";

    public const string REMOVE_ANALOGUES_BY_NET_IDS = "remove/analogues";

    public const string REMOVE_COMPONENT_BY_NET_IDS = "remove/component";

    public const string GET_PRODUCT_AVAILABILITIES_ALL = "availabilities/all";

    public const string GET_FILTERED_BY_PRODUCT_GROUP_NET_ID = "by/product/groups/filtered/get";
}
