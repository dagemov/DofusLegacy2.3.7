using Microsoft.Extensions.DependencyInjection;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Abstractions.Spells;
using RollblackLegacy.Admin.Application.Services;
using RollblackLegacy.Admin.Application.Services.ClientIdentity;
using RollblackLegacy.Admin.Application.Services.Items;

namespace RollblackLegacy.Admin.Application.DependencyInjection;

public static class AdminApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<IClientItemIdentityReadService, ClientItemIdentityReadService>();
        services.AddScoped<IItemsAdminReadService, ItemsAdminReadService>();
        services.AddScoped<IItemsAdminWriteService, ItemsAdminWriteService>();
        services.AddScoped<IItemEffectsAdminService, ItemEffectsAdminService>();
        services.AddScoped<IItemPublicationManifestService, ItemPublicationManifestService>();
        services.AddScoped<ISpellsAdminReadService, SpellsAdminReadService>();
        services.AddScoped<IItemSetsAdminReadService, ItemSetsAdminReadService>();

        return services;
    }
}
