using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Application.Services;
using RollblackLegacy.Website.Infrastructure.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

string apiBaseUrl = builder.Configuration["Website:ApiBaseUrl"]
    ?? builder.Configuration["WEBSITE_API_BASE_URL"]
    ?? "http://localhost:5074";

builder.Services.AddHttpClient<IOneLauncherApiClient, OneLauncherApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
builder.Services.AddScoped<IAccountLoginService, AccountLoginService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
