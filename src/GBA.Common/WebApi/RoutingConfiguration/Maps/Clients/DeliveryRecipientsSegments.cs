namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class DeliveryRecipientsSegments {
    public const string GET_ALL_RECIPIENTS_BY_CLIENT_NET_ID = "all/client";

    public const string GET_ALL_RECIPIENTS_DELETED_BY_CLIENT_NET_ID = "all/client/deleted";

    public const string GET_ALL_DELIVERY_RECIPIENTS_BY_CURRENT_CLIENT = "all/current";

    public const string ADD_NEW = "new";

    public const string REMOVE = "remove";

    public const string RETURN_REMOVE = "return/remove";
}