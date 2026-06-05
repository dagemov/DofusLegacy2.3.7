using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RollblackLegacy.Admin.Application.Abstractions;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Infrastructure.Configuration;
using RollblackLegacy.Admin.Infrastructure.Data;
using RollblackLegacy.Admin.Infrastructure.Items;
using RollblackLegacy.Admin.Infrastructure.Spells;
using RollblackLegacy.Admin.Infrastructure.Services;
using RollblackLegacy.Admin.Infrastructure.Services.ClientIdentity;
using RollblackLegacy.Admin.Application.Abstractions.Publication;
using RollblackLegacy.Admin.Infrastructure.Services.Items;
using RollblackLegacy.Admin.Infrastructure.Services.Publication;
using RollblackLegacy.Admin.Infrastructure.Services.Spells;

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
        services.Configure<AdminClientPublicationOptions>(options =>
        {
            options.ClientRootPath = configuration
                .GetValue<string>("AdminClientPublication:ClientRootPath") ?? string.Empty;
        });
        services.Configure<AdminClientIdentityOptions>(options =>
        {
            options.ClientRootPath = configuration
                .GetValue<string>("AdminClientIdentity:ClientRootPath") ?? string.Empty;
            options.AdminAngularRootPath = configuration
                .GetValue<string>("AdminClientIdentity:AdminAngularRootPath") ?? string.Empty;
        });

        services.AddScoped<AdminDbConnectionFactory>();
        services.AddScoped<IClientItemIdentityRepository, MySqlClientItemIdentityRepository>();
        services.AddSingleton<AdminProtocolCatalog>();
        services.AddSingleton<IClientItemSourceReader, FileSystemClientItemSourceReader>();
        services.AddSingleton<IItemEffectsCodec, ItemEffectsCodecAdapter>();
        services.AddSingleton<IItemEffectNameResolver, ItemEffectNameResolver>();
        services.AddSingleton<EffectsEnumCatalogReader>();
        services.AddSingleton<IItemEffectsCharacteristicCatalog, ItemEffectsCharacteristicCatalog>();
        services.AddSingleton<IItemEffectsCatalog, ItemEffectsCatalog>();
        services.AddSingleton<IItemClientPublicationInspector, FileSystemItemClientPublicationInspector>();
        services.AddSingleton<IStagingPublicationPackageProbe, StagingPublicationPackageProbe>();
        services.AddSingleton<IPublicationBackupStatusService, FileSystemPublicationBackupStatusService>();
        services.AddScoped<IAdminDatabaseHealthService, MySqlAdminDatabaseHealthService>();
        services.AddScoped<IItemsAdminReadRepository, ItemsAdminReadRepository>();
        services.AddScoped<IItemSetsAdminReadRepository, ItemSetsAdminReadRepository>();
        services.AddScoped<IItemsAdminWriteRepository, ItemsAdminWriteRepository>();
        services.AddScoped<IItemEffectsAdminRepository, ItemEffectsAdminRepository>();
        services.AddSingleton<ReferenceSpellCatalogReader>();
        services.AddScoped<ISpellsAdminReadRepository, SpellsAdminReadRepository>();
        services.AddSingleton<IItemPreviewStateResolver, FileSystemItemPreviewStateResolver>();
        services.AddSingleton<IItemAppearancePreviewStateResolver, FileSystemItemAppearancePreviewStateResolver>();

        return services;
    }
}
