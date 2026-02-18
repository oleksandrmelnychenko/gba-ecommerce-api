namespace GBA.Common.WebApi.RoutingConfiguration.Maps;

public static class GroupedPaymentTasksSegments {
    public const string GET_ALL_FOR_CURRENT_DATE = "all";

    public const string GET_ALL_FOR_FUTURE_FROM_DATE = "all/future";

    public const string GET_ALL_FOR_PAST_FROM_DATE = "all/past";

    public const string GET_ALL_FILTERED = "all/filtered";

    public const string GET_ALL_AVAILABLE_FOR_PAYMENT_FILTERED = "all/available/filtered";
}