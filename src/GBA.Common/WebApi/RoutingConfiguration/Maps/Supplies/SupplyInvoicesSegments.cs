namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class SupplyInvoicesSegments {
    public const string ADD_OR_UPDATE_FROM_FILE = "update/file";

    public const string UPLOAD_PRODUCT_SPECIFICATION = "specification/upload";

    public const string SET_STATUS_IS_SHIPPED = "set/shipped";

    public const string UPDATE = "update";

    public const string UPDATE_ORDER_ITEMS = "items/update";

    public const string UPDATE_VAT_PERCENTS = "items/update/vat";

    public const string DELETE = "delete/document";

    public const string GET_BY_NET_ID_WITH_ITEMS = "items/get";

    public const string GET_PZ_DOCUMENT_FOR_PRINTING = "get/documents/pz";

    public const string UPLOAD_DOCUMENTS = "upload/documents";

    public const string GET_ALL_BY_CONTAINER_NET_ID = "all/container";

    public const string GET_COUNT_BY_CONTAINER_NET_ID = "container/count";

    public const string DELETE_BY_NET_ID = "delete";

    public const string APPROVED = "approved";

    public const string GET_BY_SERVICES_NET_ID = "get/by/services";

    public const string GET_ALL_SPENDING = "all/spending/get";

    public const string ADD_DOCUMENTS = "documents/add";

    public const string ADD_DOCUMENTS_ORDER = "order/documents/add";
}