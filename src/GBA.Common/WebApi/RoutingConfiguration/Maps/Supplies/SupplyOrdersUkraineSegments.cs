namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public sealed class SupplyOrdersUkraineSegments {
    public const string ADD_OR_UPDATE_FROM_FILE = "new/file";

    public const string ADD_OR_UPDATE_FROM_SUPPLIER_FROM_FILE = "new/supplier/file";

    public const string PREVIEW_VALIDATE_ADD_OR_UPDATE_FROM_FILE = "new/file/preview";

    public const string ADD_OR_UPDATE_FROM_FILE_AFTER_PREVIEW_VALIDATION = "new";

    public const string ADD_NEW_FROM_TAX_FREE_PACK_LIST = "new/packlist/taxfree";

    public const string ADD_NEW_FROM_SAD = "new/packlist/sad";

    public const string UPDATE = "update";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_BY_NET_ID = "get";

    public const string DELETE_BY_NET_ID = "delete";

    public const string SET_ORDER_PLACED_BY_NET_ID = "set/placed";

    public const string GET_ALL_PROTOCOL_KEYS = "protocols/keys/all";

    public const string ADD_VAT_PERCENT = "vat/percent/add";

    public const string UPDATE_ITEM = "item/update";

    public const string MANAGE_DOCUMENTS = "documents/manage";

    public const string ADD_DELIVERY_EXPENSES = "new/delivery-expenses";

    public const string UPDATE_DELIVERY_EXPENSES = "update/delivery-expenses";
}