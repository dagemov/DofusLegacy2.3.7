using Microsoft.Extensions.DependencyInjection;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Services;

namespace RollblackLegacy.Admin.Application.DependencyInjection;

public static class AdminApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<IItemsAdminReadService, ItemsAdminReadService>();
        services.AddScoped<IItemsAdminWriteService, ItemsAdminWriteService>();

        return services;
    }
}
