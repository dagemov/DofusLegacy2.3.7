using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RollblackLegacy.Admin.Application.Abstractions;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Infrastructure.Configuration;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Items;
using RollblackLegacy.Admin.Infrastructure.Services;
using RollblackLegacy.Admin.Infrastructure.Services.Items;

namespace RollblackLegacy.Admin.Infrastructure.DependencyInjection;

public static class AdminInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AdminDatabaseOptions>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("SunshineAdmin") ?? string.Empty;
            options.AllowDevelopmentPlaceholderConnectionString = configuration
                .GetValue<bool>("AdminDatabase:AllowDevelopmentPlaceholderConnectionString");
        });

        services.AddScoped<AdminDbConnectionFactory>();
        services.AddSingleton<AdminProtocolCatalog>();
        services.AddSingleton<IItemEffectsCodec, ItemEffectsCodecAdapter>();
        services.AddSingleton<IItemEffectNameResolver, ItemEffectNameResolver>();
        services.AddSingleton<IItemEffectsCharacteristicCatalog, ItemEffectsCharacteristicCatalog>();
        services.AddScoped<IAdminDatabaseHealthService, MySqlAdminDatabaseHealthService>();
        services.AddScoped<IItemsAdminReadRepository, ItemsAdminReadRepository>();
        services.AddScoped<IItemsAdminWriteRepository, ItemsAdminWriteRepository>();
        services.AddScoped<IItemEffectsAdminRepository, ItemEffectsAdminRepository>();
        services.AddSingleton<IItemPreviewStateResolver, FileSystemItemPreviewStateResolver>();

        return services;
    }
}
