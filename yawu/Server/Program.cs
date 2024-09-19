using Microsoft.AspNetCore.ResponseCompression;
using Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

app.MapGet("/token", (JwtService jwtService) =>
    {
        var hubIdentifier = new HubIdentifier { ConnectionId = "e7b8a9d2-3c4e-4f8b-9a6e-1f2d3c4b5a6e" };
        var token = jwtService.GenerateToken(hubIdentifier);
        return Results.Ok(token);
    })
    .WithName("GenerateToken")
    .WithOpenApi();

app.MapHub<UpdatesHub>("/updateshub");

app.Run();