using Microsoft.Extensions.DependencyInjection;
using RollblackLegacy.Auth.Abstractions;
using RollblackLegacy.Auth.Infrastructure;
using RollblackLegacy.Auth.Services;

namespace RollblackLegacy.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRollblackLegacyAuth(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton(new LegacyAuthDbConnectionFactory(connectionString));
        services.AddScoped<ILegacyAccountRepository, LegacyAuthAccountRepository>();
        services.AddSingleton<ISunshinePasswordHasher, SunshinePasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
