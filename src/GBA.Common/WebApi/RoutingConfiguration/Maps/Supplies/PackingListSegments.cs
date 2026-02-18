namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class PackingListSegments {
    public const string UPDATE = "update";

    public const string UPDATE_VATS = "update/vats";

    public const string UPDATE_PLACEMENT_INFORMATION = "update/placement";

    public const string ADD_OR_UPDATE_FROM_FILE = "new/file";

    public const string UPDATE_IS_READY_TO_PLACED = "item/readytoplaced/update";

    public const string UPDATE_IS_READY_TO_PLACED_TO_ALL = "item/readytoplaced/update/all";

    public const string UPDATE_PLACEMENT_PACKING_LIST = "placement/info/update";

    public const string UPDATE_FROM_FILE = "update/file";

    public const string UPLOAD_DOCUMENTS = "upload/documents";

    public const string GET_BY_INVOICE_NET_ID = "get";

    public const string DELETE_BY_NET_ID = "delete";

    public const string GET_PRODUCTS_SPECIFICATION = "specification/products/get";

    public const string GET_PACKING_LIST_SPECIFICATION_URL = "specification/get";

    public const string GET_ALL_UNSHIPPED = "unshipped/all";
}