using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Server;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddSignalR();
services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});
services.AddJwtService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

app.MapGet("/token", ([FromServices] JwtService jwtService) =>
    {
        var hubIdentifier = new HubIdentifier { ConnectionId = Constants.ConnectionId };
        var token = jwtService.GenerateToken(hubIdentifier);
        return Results.Ok(new TokenResult
        {
            Token = token
        });
    })
    .WithName("GenerateToken")
    .WithOpenApi();

app.MapHub<UpdatesHub>("/updateshub");

app.Run();