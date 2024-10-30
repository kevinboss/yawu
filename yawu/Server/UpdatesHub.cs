using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Server;

public class UpdatesHub(JwtService jwtService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var connectionId = Constants.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"WebClients_{connectionId}");
        await base.OnConnectedAsync();
    }

    public async Task Packages(Package[] packages, string token)
    {
        var hubIdentifier = jwtService.ValidateToken(token);
        await Clients.Group($"WebClients_{hubIdentifier.ConnectionId}").SendAsync("Packages", packages);
    }

    public async Task PackageUpdates(PackageUpdate[] packageUpdates, string token)
    {
        var hubIdentifier = jwtService.ValidateToken(token);
        await Clients.Group($"WebClients_{hubIdentifier.ConnectionId}").SendAsync("PackageUpdates", packageUpdates);
    }
}