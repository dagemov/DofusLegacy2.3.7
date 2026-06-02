using RollblackLegacy.Admin.Api.Endpoints;
using RollblackLegacy.Admin.Api.ErrorHandling;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using RollblackLegacy.Admin.Infrastructure.Items;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.local.json",
        optional: true,
        reloadOnChange: true);
}

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is invalid.",
                Type = "https://httpstatuses.com/400",
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails);
        };
    });

builder.Services.AddExceptionHandler<AdminApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddAdminApplication();
builder.Services.AddAdminInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
ConfigureAdminStaticAssetPath(
    AdminRepositoryPathResolver.ResolveAdminAngularItemPreviewsRoot(app.Environment.ContentRootPath),
    app,
    "/assets/item-previews");
ConfigureAdminStaticAssetPath(
    AdminRepositoryPathResolver.ResolveAdminAngularManualItemsRoot(app.Environment.ContentRootPath),
    app,
    "/manual-assets/items");
app.MapAdminHealthEndpoints();
app.MapControllers();

app.Run();

static void ConfigureAdminStaticAssetPath(string physicalDirectory, WebApplication app, string requestPath)
{
    if (!Directory.Exists(physicalDirectory))
    {
        return;
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(physicalDirectory),
        RequestPath = requestPath
    });
}
