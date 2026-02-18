namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class StoragesSegments {
    public const string ADD_NEW = "new";

    public const string GET_ALL = "all";

    public const string GET_ALL_NON_DEFECTIVE = "all/nondefective";

    public const string GET_ALL_DEFECTIVE = "all/defective";

    public const string GET_ALL_FOR_RETURNS = "all/returns";

    public const string GET_ALL_FOR_RETURNS_FILTERED = "all/returns/filtered";

    public const string GET_BY_NET_ID = "get";

    public const string GET_TOTAL_PRODUCTS_COUNT_BY_NET_ID = "get/products/total";

    public const string GET_ALL_AVAILABLE_BY_STORAGE_NET_ID_FILTERED = "all/available/filtered";

    public const string GET_ALL_INCOMED_BY_STORAGE_NET_ID_FILTERED = "all/incomed/filtered";

    public const string GET_ALL_INCOMED_SUPPLIER_INFO_BY_STORAGE_NET_ID_FILTERED = "all/incomed/supplier/filtered";

    public const string GET_ALL_FOR_ECOMMERCE = "all/ecommerce";

    public const string UPDATE = "update";

    public const string SET_STORAGE_FOR_ECOMMERCE = "ecommerce/set";

    public const string SET_STORAGE_PRIORITY = "priority";

    public const string UNSELECT_STORAGE_FOR_ECOMMERCE = "ecommerce/unselect";

    public const string DELETE = "delete";

    public const string EXPORT_ALL_PRODUCTS_BY_STORAGE_DOCUMENT = "document/export";

    public const string GET_ALL_STORAGES_WITH_ORGANIZATIONS = "get/all";

    public const string GET_ALL_STORAGES_FILTERED_BY_ORGANIZATION = "get/all/filtered";
}