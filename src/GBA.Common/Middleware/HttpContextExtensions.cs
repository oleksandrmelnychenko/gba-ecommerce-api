using System;
using GBA.Common.Logging;
using Microsoft.AspNetCore.Http;

namespace GBA.Common.Middleware;

public static class HttpContextExtensions {
    public static Guid GetUserNetId(this HttpContext context) {
        return context.Items.TryGetValue(UserNetIdMiddleware.NetIdKey, out object value) && value is Guid netId
            ? netId
            : Guid.Empty;
    }

    public static string GetCorrelationId(this HttpContext context) {
        return context.Items.TryGetValue(LoggingDefaults.CorrelationIdItemKey, out object value) && value is string correlationId
            ? correlationId
            : string.Empty;
    }
}
