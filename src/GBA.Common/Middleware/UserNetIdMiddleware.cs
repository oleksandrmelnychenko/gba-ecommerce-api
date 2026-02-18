using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GBA.Common.Middleware;

public class UserNetIdMiddleware {
    private readonly RequestDelegate _next;
    public const string NetIdKey = "UserNetId";
    private const string NetIdClaimType = "NetId";

    public UserNetIdMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        if (context.User.Identity?.IsAuthenticated == true) {
            // Avoid LINQ allocation - iterate directly
            foreach (Claim claim in context.User.Claims) {
                if (claim.Type == NetIdClaimType) {
                    if (Guid.TryParse(claim.Value, out Guid netId)) {
                        context.Items[NetIdKey] = netId;
                    }
                    break;
                }
            }
        }
        await _next(context);
    }
}
