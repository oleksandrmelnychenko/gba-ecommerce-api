namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ClientsSegments {
    public const string ADD_NEW = "new";

    public const string GET_BY_NET_ID = "get";

    public const string GET_BY_SUB_CLIENT_NET_ID = "get/subclient";

    public const string GET_ALL = "all";

    public const string GET_ALL_FROM_SEARCH = "search/all";

    public const string GET_ALL_CLIENTS_DOCUMENT = "document";

    public const string GET_ALL_REGISTERED_FROM_ECOMMERCE = "all/ecommerce";

    public const string GET_ALL_SUB_CLIENTS = "all/subclients/client";

    public const string GET_ALL_CLIENT_SUB_CLIENTS = "all/clientsubclients/client";

    public const string GET_ALL_SHOP_CLIENTS = "all/shop";

    public const string GET_ALL_WITH_DEBT = "all/debt";

    public const string GET_TOTAL_AMOUNT = "get/total";

    public const string GET_TOP_BY_SALES = "get/top";

    public const string GET_TOP_BY_ONLINE_ORDERS = "get/top/shop";

    public const string GET_AVG_BY_PRODUCT = "average/product";

    public const string GET_GROUPED_DEBTS = "get/debt/grouped";

    public const string GET_GROUPED_DEBT_TOTALS_BY_STRUCTURE = "get/debt/total/structure";

    public const string GET_GROUPED_DEBT_TOTALS_BY_STRUCTURE_WITH_ROOT = "get/debt/total";

    public const string UPDATE = "update";

    public const string UPDATE_PASSWORD = "update/password";

    public const string DELETE = "delete";

    public const string SWITCH_ACTIVE_STATE = "switch/active";

    public const string GET_ALL_SUB_CLIENT_CLIENT_AGREEMENTS = "clientagreements/all/sub/client";

    public const string IS_SUB_CLIENTS_HAS_AGREEMENTS = "subclients/clientagreements/any";

    public const string GET_DEBT_INFO_BY_CLIENT_NET_ID = "get/debtinfo";

    public const string GET_ALL_MANUFACTURERS = "all/manufacturers";

    public const string GET_ALL_FROM_SEARCH_BY_SERVICE_PAYERS = "payers/search/all";

    public const string GET_ALL_FROM_SEARCH_BY_NAME_AND_REGION_CODE = "search/all/nameandcode";

    public const string GET_ALL_FROM_SEARCH_BY_SALES = "search/all/sales";

    public const string GET_ALL_ORDER_ITEMS_BY_CLIENT = "get/orders/items";

    public const string GET_CLIENTS_PURCHASE_ACTIVITY = "get/purchase/activity";

    public const string GET_CLIENTS_NOT_TO_BUY_ANYTHING = "get/purchase/missing";

    public const string ADD_NEW_TEMPORARY_CLIENT = "new/temp";

    public const string SET_IS_FOR_RETAIL = "retail/set";

    public const string GET_ALL_CLIENT_GROUPS = "all/groups";

    public const string UPDATE_CLIENT_ORDER_EXPIRE_DAYS = "update/order/expire";

    public const string UPDATE_CLIENT_GROUP = "update/client/group";

    public const string UPDATE_CLIENT_CLIENT_GROUP = "update/clientgroup";

    public const string ADD_ECOMMERCE_CLIENT = "add/ecommerce/client";

    public const string ADD_CLIENT_GROUP = "new/group";

    public const string GET_WORKPLACES_BY_GROUP_NETID = "all/workplaces/by/group";

    public const string ADD_CLIENT_WORKPLACE = "new/workplace";

    public const string GET_WORKPLACES_BY_MAIN_CLIENT_NETID = "all/workplaces/by/client";

    public const string REMOVE_CLIENT_GROUP_FROM_CLIENT = "remove/clientgroup/from";

    public const string REMOVE_CLIENT_WORKPLACE = "remove/workplace";

    public const string UPDATE_CLIENT_WORKPLACE = "update/workplace";
}