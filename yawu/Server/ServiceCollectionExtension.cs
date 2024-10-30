namespace Server;

public static class ServiceCollectionExtension
{
    // Ensure the secret key is at least 32 bytes long
    private const string SecretKey = "your_very_long_secret_key_that_is_at_least_32_bytes";

    public static IServiceCollection AddJwtService(this IServiceCollection services)
    {
        services.AddScoped<JwtService>(_ => new JwtService(SecretKey, "your_issuer", "your_audience"));

        return services;
    }
}