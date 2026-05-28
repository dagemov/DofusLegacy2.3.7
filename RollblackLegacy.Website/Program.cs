using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Application.Services;
using RollblackLegacy.Website.Infrastructure.Persistence;
using RollblackLegacy.Website.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

string connectionString = builder.Configuration.GetConnectionString("SunshineAuth")
    ?? throw new InvalidOperationException("Connection string 'SunshineAuth' is missing.");

builder.Services.AddSingleton(new LegacyWebsiteDbConnectionFactory(connectionString));
builder.Services.AddScoped<ILegacyAccountRepository, LegacyAuthAccountRepository>();
builder.Services.AddScoped<ISunshinePasswordHasher, SunshinePasswordHasher>();
builder.Services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
