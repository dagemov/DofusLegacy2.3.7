using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rollback.Admin.Configuration;

namespace Rollback.Admin.Services;

public static class AdminRegistrationExtensions
{
    public static IServiceCollection AddRollbackAdmin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RollbackAdminDatabasesOptions>(configuration.GetSection("RollbackDatabases"));
        services.AddSingleton<Infrastructure.AdminDbConnectionFactory>();
        services.AddSingleton<ClientDataPathResolver>();
        services.AddSingleton<ClientReferenceResolverService>();
        services.AddSingleton<I18nCatalogService>();
        services.AddSingleton<FfdecItemScriptExtractor>();
        services.AddSingleton<FfdecSpellLevelScriptExtractor>();
        services.AddSingleton<ClientItemMetadataService>();
        services.AddSingleton<ClientEffectDefinitionService>();
        services.AddSingleton<SpellClientPresentationCompatibilityService>();
        services.AddSingleton<SpellTooltipFallbackService>();
        services.AddSingleton<GameAssetPreviewService>();
        services.AddSingleton<ReferenceItemCatalogService>();
        services.AddSingleton<ReferenceSpellCatalogService>();
        services.AddSingleton<CustomItemClassificationService>();
        services.AddScoped<AdminBootstrapService>();
        services.AddScoped<AdminSpecialSpellAssignmentService>();
        services.AddScoped<AdminRuntimeRevisionService>();
        services.AddScoped<AdminEntityTextOverrideService>();
        services.AddScoped<AdminEntityAssetOverrideService>();
        services.AddScoped<AdminEntityClientMetadataService>();
        services.AddScoped<ClassicSpellDomainService>();
        services.AddScoped<MonsterAdminService>();
        services.AddScoped<MonsterCatalogService>();
        services.AddScoped<MonsterFamilyCatalogService>();
        services.AddScoped<MonsterGroupAdminService>();
        services.AddScoped<MapSpawnAdminService>();
        services.AddScoped<AdminItemCatalogService>();
        services.AddScoped<ItemAppearanceCatalogService>();
        services.AddScoped<ItemAppearanceResolverService>();
        services.AddScoped<ItemIdentityDiagnosticService>();
        services.AddScoped<ItemIdentityCorrectionService>();
        services.AddScoped<ItemClientPublishService>();
        services.AddSingleton<GameEffectDisplayService>();
        services.AddScoped<GameEffectEditorService>();
        services.AddScoped<SpellEffectCatalogService>();
        services.AddScoped<ItemAdminService>();
        services.AddScoped<SetClientPublishService>();
        services.AddScoped<SetAdminService>();
        services.AddScoped<SpellAdminSchemaService>();
        services.AddScoped<SpellClientPublishService>();
        services.AddScoped<SpellAdminService>();
        services.AddScoped<SpellPublishOrchestrator>();
        services.AddScoped<NpcClientPublishService>();
        services.AddScoped<NpcSkinCatalogService>();
        services.AddScoped<NpcAdminService>();
        services.AddSingleton<NpcVendorCatalogService>();
        services.AddScoped<NpcVendorInventorySyncService>();
        services.AddScoped<NpcVendorAdminService>();
        services.AddScoped<CharacterAdminService>();
        return services;
    }
}
