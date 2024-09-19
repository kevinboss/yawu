using System.Security.Cryptography;

namespace Server;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddJwtService(this IServiceCollection services)
    {
        var secretKey = new byte[256 / 8];
        RandomNumberGenerator.Fill(secretKey);
        var secret = Convert.ToBase64String(secretKey);

        services.AddScoped<JwtService>(_ => new JwtService(secret, "your_issuer", "your_audience"));

        return services;
    }
}