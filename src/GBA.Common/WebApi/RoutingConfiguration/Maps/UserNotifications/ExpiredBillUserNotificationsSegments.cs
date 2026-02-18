namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class ExpiredBillUserNotificationsSegments {
    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_BY_NET_ID = "get";

    public const string GET_LOCKED_NOTIFICATION_BY_CURRENT_USER = "get/locked";

    public const string LOCK_NOTIFICATION_BY_NET_ID = "lock";

    public const string UNLOCK_NOTIFICATION_BY_NET_ID = "unlock";

    public const string APPLY_DEFER_ACTION_TO_NOTIFICATION_BY_NET_ID = "action/defer";

    public const string APPLY_SHIFT_ACTION_TO_NOTIFICATION_BY_NET_ID = "action/shift";
}