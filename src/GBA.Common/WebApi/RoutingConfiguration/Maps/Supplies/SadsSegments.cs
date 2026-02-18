namespace GBA.Common.WebApi.RoutingConfiguration.Maps.Supplies;

public static class SadsSegments {
    public const string ADD_OR_UPDATE = "update";

    public const string ADD_OR_UPDATE_FROM_SALE = "update/sale";

    public const string GET_BY_NET_ID = "get";

    public const string DELETE_BY_NET_ID = "delete";

    public const string GET_BY_NET_ID_WITH_PRODUCT_SPECIFICATION = "specification/products/get";

    public const string UPLOAD_PRODUCT_SPECIFICATION = "specification/upload";

    public const string UPLOAD_NEW_PRODUCT_SPECIFICATION = "new/specification/upload";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_ALL_NOT_SENT = "all/notsent";

    public const string GET_ALL_NOT_SENT_FROM_SALE = "all/notsent/sale";

    public const string GET_ALL_SENT = "all/sent";

    public const string UPLOAD_DOCUMENTS = "documents/upload";

    public const string EXPORT_DOCUMENTS_FOR_PRINTING = "documents/export";

    public const string REMOVE_DOCUMENT = "documents/remove";
}