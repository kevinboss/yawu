using Microsoft.AspNetCore.SignalR.Client;
using WebClient;
using WebClient.Components;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
services.AddScoped<JsConsole>();
services.AddSingleton(_ => new HubConnectionBuilder()
    .WithUrl("http://localhost:5275/updatesHub")
    .WithAutomaticReconnect()
    .Build());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<HubConnection>().StartAsync();

app.Run();

