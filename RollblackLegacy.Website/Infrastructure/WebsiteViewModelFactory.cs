using RollblackLegacy.Website.Contracts.Account;
using RollblackLegacy.Website.Contracts.Branding;
using RollblackLegacy.Website.Contracts.Components;
using RollblackLegacy.Website.Contracts.Home;

namespace RollblackLegacy.Website.Infrastructure;

public static class WebsiteViewModelFactory
{
    public static HomePageViewModel CreateHomePage(IConfiguration configuration)
    {
        BrandIdentityViewModel brand = CreateBrand(configuration);

        return new HomePageViewModel
        {
            Brand = brand,
            HeroTitle = "Una vuelta sobria al Mundo de los Doce",
            HeroSubtitle = "Progression old-school, atmosfera oscura y tooling moderno para una experiencia Legacy mas estable.",
            HeroActions =
            [
                new ButtonAtomViewModel
                {
                    Label = "Crear cuenta",
                    Href = "/account/register",
                    Variant = "primary",
                    Icon = "fa-solid fa-user-plus",
                },
                new ButtonAtomViewModel
                {
                    Label = "Launcher proximamente",
                    Href = configuration["Website:LauncherDownloadUrl"] ?? "#",
                    Variant = "secondary",
                    Icon = "fa-solid fa-download",
                },
                new ButtonAtomViewModel
                {
                    Label = "Discord proximamente",
                    Href = configuration["Website:DiscordUrl"] ?? "#",
                    Variant = "ghost",
                    Icon = "fa-brands fa-discord",
                    IsExternal = true,
                },
            ],
            NewsItems =
            [
                new NewsItemViewModel
                {
                    Label = "Placeholder",
                    Title = "Roadmap del servidor",
                    Summary = "La web arranca con registro, home publica y base visual inspirada en el launcher. Noticias reales iran aqui.",
                },
                new NewsItemViewModel
                {
                    Label = "Setup",
                    Title = "Infraestructura alineada con Sunshine",
                    Summary = "El modulo web se conecta a la misma base auth y documenta el flujo de hash compatible con el emulador.",
                },
                new NewsItemViewModel
                {
                    Label = "Estado",
                    Title = "Paneles preparados para futuras metricas",
                    Summary = "El layout ya reserva espacio para status real de auth/world, descarga del launcher y enlaces de comunidad.",
                },
            ],
            ServerStatuses =
            [
                new ServerStatusViewModel
                {
                    Name = "Auth",
                    State = "Placeholder",
                    Summary = "Pendiente de telemetria en vivo desde el proceso Sunshine.",
                    IsOnline = false,
                },
                new ServerStatusViewModel
                {
                    Name = "World",
                    State = "Placeholder",
                    Summary = "La tarjeta ya esta lista para enlazar uptime y poblacion.",
                    IsOnline = false,
                },
                new ServerStatusViewModel
                {
                    Name = "Registro web",
                    State = "Activo",
                    Summary = "Alta de cuentas conectada a la base auth del servidor.",
                    IsOnline = true,
                },
            ],
        };
    }

    public static RegisterAccountPageViewModel CreateRegisterPage(
        IConfiguration configuration,
        RegisterAccountInputModel? form = null,
        RegisterAccountResultViewModel? result = null)
    {
        return new RegisterAccountPageViewModel
        {
            Brand = CreateBrand(configuration),
            Form = form ?? new RegisterAccountInputModel(),
            Result = result,
            Title = "Crear cuenta Legacy",
            Subtitle = "El registro escribe en la base auth real del servidor y utiliza el hashing compatible con Sunshine.",
            SecurityHint = "MVP actual: el emulador guarda MD5(password) en accounts.Password y reutiliza el correo como respuesta secreta inicial.",
            DiscordUrl = configuration["Website:DiscordUrl"] ?? "#",
        };
    }

    public static BrandIdentityViewModel CreateBrand(IConfiguration configuration)
    {
        return new BrandIdentityViewModel
        {
            Name = configuration["Website:BrandName"] ?? "Rollblack Legacy",
            Tagline = configuration["Website:Tagline"] ?? "Legacy launcher-inspired experience",
            Description = configuration["Website:Description"] ?? "Servidor publico con UI inspirada en el launcher y pipeline de cuentas conectado al auth real.",
            LogoPath = "/images/branding/rollblack-mascot.svg",
            FaviconPath = "/images/branding/rollblack-mascot.svg",
        };
    }
}
