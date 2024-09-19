using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Server;

public class UpdatesHub(JwtService jwtService) : Hub
{
    public async Task PackagesUpdated(Package[] packages, string token)
    {
        var connectionId = jwtService.ValidateToken(token);
        await Clients.Group($"WebClients_{connectionId}").SendAsync("PackagesUpdated", packages);
    }
}