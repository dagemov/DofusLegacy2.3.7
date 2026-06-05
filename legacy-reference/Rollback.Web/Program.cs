using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Rollback.Admin.Services;
using Rollback.Accounts.Configuration;
using Rollback.Accounts.Services;
using Rollback.Web.Services;

namespace Rollback.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<RollbackDatabasesOptions>(builder.Configuration.GetSection("RollbackDatabases"));
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<AdminAssetUploadService>();
        builder.Services.AddScoped<IAccountPortalService, AccountPortalService>();
        builder.Services.AddRollbackAdmin(builder.Configuration);
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Rollback.Portal";
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        var assetPreviewService = app.Services.GetRequiredService<GameAssetPreviewService>();
        if (!string.IsNullOrWhiteSpace(assetPreviewService.ItemBitmapDirectory) &&
            Directory.Exists(assetPreviewService.ItemBitmapDirectory))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assetPreviewService.ItemBitmapDirectory),
                RequestPath = "/game-assets/items",
            });
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        using (var scope = app.Services.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
            //bootstrap.EnsureSchemaAsync().GetAwaiter().GetResult();
        }

        app.Run();
    }
}
