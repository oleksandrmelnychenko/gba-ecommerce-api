namespace GBA.Common.Logging;

/// <summary>
/// Shared constants for the structured-logging stack (correlation id, scope keys).
/// </summary>
public static class LoggingDefaults {
    /// <summary>HTTP header carrying the per-request correlation id (in and out).</summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>Key under which the correlation id is stashed in <c>HttpContext.Items</c>.</summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    /// <summary>NLog scope/structured-property name for the correlation id.</summary>
    public const string CorrelationIdProperty = "CorrelationId";

    /// <summary>Max number of request-body bytes captured when logging a failed request.</summary>
    public const int MaxLoggedBodyBytes = 16 * 1024;
}
