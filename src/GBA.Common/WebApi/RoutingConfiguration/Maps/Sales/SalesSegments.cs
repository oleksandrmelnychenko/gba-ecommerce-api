namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class SalesSegments {
    public const string ADD_NEW = "new";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_ALL_FILTERED_FROM_PL_UK_CLIENTS = "all/filtered/pl-uk";

    public const string GET_ALL_FILTERED_BY_TRANSPORTER = "all/transporter/filtered";

    public const string GET_ALL_FROM_E_COMMERCE_FROM_PL_UK_CLIENTS = "all/client/pl-uk";

    public const string GET_ALL_SUB_CLIENTS_SALES_BY_CLIENT_NET_ID = "all/subclients/client";

    public const string GET_ALL_BY_CLIENT_NET_ID = "all/client";

    public const string GET_SALES_REGISTER_BY_CLIENT_NET_ID = "all/register";

    public const string GET_ALL_FOR_RETURNS_FROM_SEARCH = "all/returns/search";

    public const string GET_ALL_BY_LIFE_CYCLE_TYPE = "all/lifecycle";

    public const string GET_ALL_TOTAL_AMOUNT = "all/total";

    public const string GET_ALL_ITEMS_WITH_PRODUCT_LOCATIONS = "all/items/locations";

    public const string GET_TOTAL_BY_YEAR = "year/total";

    public const string GET_TOTALS_BY_SALES_MANAGERS = "managers/total";

    public const string GET_BY_NET_ID = "get";

    public const string GET_BY_NET_ID_ECOMMERCE = "store/get";

    public const string GET_CURRENT_BY_CLIENT_AGREEMENT_NET_ID = "get/current";

    public const string GET_ALL_VALID_CLIENT_SHOPPING_CARTS = "carts/all";

    public const string GET_CURRENT_NOT_MERGED_BY_CLIENT_AGREEMENT_NET_ID = "get/current/unmerged";

    public const string GET_BY_NET_ID_WITH_SHIFTED_ITEMS = "get/shifted";

    public const string GET_BY_NET_ID_WITH_SHIFTED_DOCUMENT_ITEMS = "get/shifted/document";

    public const string GET_BY_NET_ID_WITH_SHIFTED_HISTORY_DOCUMENT_ITEMS = "get/shifted/hisotry/document";

    public const string GET_BY_NET_ID_WITH_MERGED_ITEMS = "get/merged";

    public const string UPDATE_MERGED_SALE_TO_BILL = "update/merged";

    public const string GET_SALES_STATISTIC_BY_DATE_RANGE_AND_USER_NET_ID = "get/statistic";

    public const string GET_SALE_INVOICE_DOCUMENT_XLSX_URL = "get/document";

    public const string GET_LAST_SALE_INVOICE_DOCUMENT_XLSX_URL = "get/last/document";

    public const string GET_SALE_INVOICE_DOCUMENT_HISTORY_XLSX_URL = "get/document/history";

    public const string GET_INVOICE_FOR_PAYMENT_FOR_SALE_BY_NET_ID = "get/payment/document";

    public const string GET_REGISTER_INVOICE = "get/register/invoice";

    public const string GET_REGISTER_INVOICE_DOCUMENT = "get/register/invoice/document";

    public const string GET_INVOICE_FOR_PAYMENT_FOR_SALE_FROM_LAST_STEP = "update/get/payment/document";

    public const string UNLOCK_SALE_BY_NET_ID = "unlock";

    public const string GET_SALE_INVOICE_PZ_DOCUMENT_XLSX_URL = "get/document/pz";

    public const string UPDATE = "update";

    public const string UPDATE_FILE = "update/file";

    public const string GET_CHANGE = "get/change";

    public const string SWITCH_SALE_UNDER_CLIENT_STRUCTURE = "switch";

    public const string UPDATE_DELIVERY_RECIPIENT = "update/recipient";

    public const string UPDATE_DELIVERY_RECIPIENT_ADDRESS = "update/recipient/address";

    public const string CALCULATE_SALE_WITH_ONE_TIME_DISCOUNT = "discount/calculate";

    public const string UPDATE_SALE_WITH_ONE_TIME_DISCOUNT = "discount/update";

    public const string UPDATE_FROM_ECOMMERCE = "update/ecommerce";

    public const string DELETE = "delete";

    public const string ECOMMERCE_SYNC_SALE_ADDED = "sync/new";

    public const string GET_BY_FEATURES = "get";

    public const string GET_CHART_BY_CLIENT = "chart/by/client";

    public const string GET_SALES_INFO_BY_MANAGERS = "get/info/by/managers";

    public const string GET_MANAGER_SALES_BY_PRODUCT_TOP = "get/managers/product/top";

    public const string SHIPMENT_LIST_FOR_SALES_PRINT_DOCUMENT = "shipment/list/print/documents/";

    public const string SHIPMENT_LIST_FOR_SALES_PRINT_DOCUMENT_HISTORY = "shipment/list/print/documents/history";

    public const string SAVE_PAYMENT_IMAGE = "payment/save";

    public const string GET_ALL_PAYMENT_IMAGES = "payment/images/get";

    public const string GET_ALL_PAYMENT_IMAGES_FILTERED = "payment/images/get/filtered";

    public const string SAVE_CONSUMERS_TTN_FILE = "save/ttn";

    public const string UPDATE_COMMENT = "update/comment";
}