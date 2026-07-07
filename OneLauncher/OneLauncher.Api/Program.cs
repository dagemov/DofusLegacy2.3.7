using OneLauncher.Api.Options;
using OneLauncher.Api.Services;
using RollblackLegacy.Auth;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("SunshineAuth")
    ?? throw new InvalidOperationException("Connection string 'SunshineAuth' is missing.");

builder.Services.AddRollblackLegacyAuth(connectionString);

builder.Services.Configure<LauncherManifestOptions>(
    builder.Configuration.GetSection(LauncherManifestOptions.SectionName));
builder.Services.Configure<PackageStorageOptions>(
    builder.Configuration.GetSection(PackageStorageOptions.SectionName));
builder.Services.Configure<ElectronUpdatesOptions>(
    builder.Configuration.GetSection(ElectronUpdatesOptions.SectionName));

string? corsFromEnv = builder.Configuration["ONELAUNCHER_CORS_ORIGINS"];
string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (string.IsNullOrWhiteSpace(corsFromEnv)
        ? ["https://rollblack-legacy.onesv.online"]
        : corsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

builder.Services.AddCors(options =>
{
    options.AddPolicy("LauncherClients", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IUpdatesXmlCatalog, UpdatesXmlCatalog>();
builder.Services.AddSingleton<ILauncherManifestService, LauncherManifestService>();
builder.Services.AddSingleton<IElectronUpdatesService, ElectronUpdatesService>();

string packageRoot = builder.Configuration["PackageStorage:RootPath"] ?? string.Empty;
if (!string.IsNullOrWhiteSpace(packageRoot) && Directory.Exists(packageRoot))
{
    builder.Services.AddSingleton<IPackageFileService, DiskPackageFileService>();
}
else
{
    builder.Services.AddSingleton<IPackageFileService, MockPackageFileService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LauncherClients");
app.UseAuthorization();
app.MapControllers();

app.Run();
