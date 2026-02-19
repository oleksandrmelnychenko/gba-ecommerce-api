using System;
using Microsoft.AspNetCore.Http;

namespace GBA.Common.Middleware;

public static class HttpContextExtensions {
    public static Guid GetUserNetId(this HttpContext context) {
        return context.Items.TryGetValue(UserNetIdMiddleware.NetIdKey, out object value) && value is Guid netId
            ? netId
            : Guid.Empty;
    }
}
