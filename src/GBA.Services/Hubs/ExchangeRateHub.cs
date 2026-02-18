using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GBA.Services.Hubs;

public sealed class ExchangeRateHub : Hub {
    public override async Task OnConnectedAsync() {
        await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception) {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SignalR Users");
        await base.OnDisconnectedAsync(exception);
    }
}