namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class SupplyOrdersSegments {
    public const string ADD_NEW = "new";

    public const string ADD_NEW_FROM_FILE = "new/file";

    public const string UPDATE = "update";

    public const string ADD_PAYMENTS = "payments/add";

    public const string GET_BY_NET_ID = "get";

    public const string DELETE_BY_NET_ID = "delete";

    public const string GET_BY_NET_ID_FOR_PLACEMENT = "get/placement";

    public const string GET_ALL = "all";

    public const string GET_ALL_FOR_PLACEMENT = "all/placement";

    public const string GET_ALL_FROM_SEARCH = "search/all";

    public const string GET_ALL_FOR_UK_ORGANIZATIONS_FILTERED = "all/uk/filtered";

    public const string GET_TOTALS = "get/total";

    public const string GET_NEAREST_SUPPLY_ARRIVAL = "arrival/nearest/get";

    public const string GET_TOTAL_ON_ITEMS = "get/items/total";

    public const string GET_ALL_PAYMENT_DELIVERY_PROTOCOL_KEYS = "payments/all/keys";

    public const string GET_ALL_INFORMATION_DELIVERY_PROTOCOL_KEYS = "informations/all/keys";

    public const string GET_ALL_SERVICE_DETAIL_ITEM_KEYS_BY_SERVICE_TYPE = "services/all/keys";

    public const string UPLOAD_PACKING_LISTS = "upload/packinglist";

    public const string UPLOAD_CREDIT_NOTE = "upload/creditnote";

    public const string UPLOAD_POLAND_PAYMENT_DELIVERY_PROTOCOL_DOCUMENTS = "payments/upload/documents";

    public const string ORDER_STATUS_IS_APPROVED = "approved";

    public const string PRINT_DOCUMENTS = "print/documents";
}