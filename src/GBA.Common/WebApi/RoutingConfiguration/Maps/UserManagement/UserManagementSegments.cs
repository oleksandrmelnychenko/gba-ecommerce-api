namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class UserManagementSegments {
    public const string SIGN_UP = "signup";

    public const string SIGN_IN = "signin";

    public const string GET_TOKEN = "token";

    public const string REFRESH_TOKEN = "token/refresh";

    public const string IS_USERNAME_AVAILABLE = "check/phone";

    public const string IS_EMAIL_AVAILABLE = "check/email";

    public const string GET_ALL_ROLES = "roles/all";

    public const string ASSIGN_USER_TO_ROLE = "roles/assign";

    public const string UNASSIGN_USER_FROM_ROLE = "roles/unassign";

    public const string GET_CLIENT_CASH_FLOW = "get/cashflow";

    public const string USERS_OLD_SHOP = "user/old/shop";
}