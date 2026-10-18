using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GBA.Ecommerce.Hubs;

/// <summary>
/// Publishes storefront-wide configuration changes to connected shoppers.
/// </summary>
[AllowAnonymous]
public sealed class StorefrontHub : Hub {
}
