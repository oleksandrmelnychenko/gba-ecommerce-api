namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ProductTransfersSegments {
    public const string GET_ALL = "all";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_BY_NET_ID = "get";

    public const string ADD_NEW = "new";

    public const string ADD_NEW_FROM_PACKING_LIST = "new/packinglist";

    public const string ADD_NEW_FROM_ACT_RECONCILIATION_ITEM = "new/reconciliation";

    public const string ADD_NEW_FROM_ACT_RECONCILIATION_ITEMS = "new/reconciliation/many";

    public const string EXPORT_PRINTING_DOCUMENT = "document/export";

    public const string ADD_FROM_FILE_NEW = "add/file";
}