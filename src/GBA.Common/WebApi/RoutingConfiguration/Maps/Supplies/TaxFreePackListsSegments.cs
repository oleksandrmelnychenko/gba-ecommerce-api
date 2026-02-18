namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class TaxFreePackListsSegments {
    public const string ADD_OR_UPDATE = "update";

    public const string ADD_OR_UPDATE_FROM_SALE = "update/sale";

    public const string GET_BY_NET_ID = "get";

    public const string DELETE_BY_NET_ID = "delete";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_ALL_NOT_SENT = "all/notsent";

    public const string GET_ALL_NOT_SENT_FROM_SALE = "all/notsent/sale";

    public const string GET_ALL_SENT = "all/sent";

    public const string BREAK_PACK_LIST_INTO_TAX_FREES = "break";

    public const string PRINT_DOCUMENTS = "print/documents";
}