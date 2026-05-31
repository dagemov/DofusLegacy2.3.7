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
            HeroBadge = "Beta abierta",
            HeroTitle = "El despertar de los Doce",
            HeroSubtitle =
                "Regresa al Mundo de los Doce en una experiencia refinada y oscura. La leyenda renace en tus manos.",
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
                    Label = "Discord",
                    Href = configuration["Website:DiscordUrl"] ?? "#",
                    Variant = "secondary",
                    Icon = "fa-brands fa-discord",
                    IsExternal = true,
                },
            ],
            Lore = new LoreSectionViewModel
            {
                Eyebrow = "Lore",
                Title = "Cronicas del olvido",
                Paragraphs =
                [
                    "Hace eras, cuando los doce dioses aun caminaban entre mortales, Rollblack fue un umbral entre la luz del Krosmoz y las sombras que devoran la memoria. "
                    + "Los viajeros que cruzaron sus puertas juraron nunca olvidar el peso de cada paso en Amakna, en Brakmar y en los mapas donde el tiempo se dobla.",
                    "Hoy el velo se adelgaza de nuevo. En esta beta, los guardianes del servidor abren un camino sobrio: progresion old-school, "
                    + "economia honesta y un launcher que une tu cuenta web, el cliente y el reino entero. "
                    + "No buscamos ruido ni promesas vacias — solo un Mundo de los Doce donde tu historia vuelva a importar.",
                ],
            },
            NewsItems =
            [
                new NewsItemViewModel
                {
                    Label = "Progresion",
                    Title = "Legacy con ritmo clasico",
                    Summary = "Curva old-school, combates tactiles y sensacion de MMORPG maduro. La beta afina balance y estabilidad antes del lanzamiento publico.",
                },
                new NewsItemViewModel
                {
                    Label = "Cuenta",
                    Title = "Una sola llave para todo",
                    Summary = "Registro web, launcher OneLauncher y auth del juego comparten la misma cuenta via API. Entra una vez, juega en todas partes.",
                },
                new NewsItemViewModel
                {
                    Label = "Beta",
                    Title = "Acceso anticipado",
                    Summary = "Fase beta abierta: crea tu cuenta, descarga el cliente desde el manifiesto y acompananos mientras el reino despierta.",
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

    public static LoginAccountPageViewModel CreateLoginPage(
        IConfiguration configuration,
        LoginAccountInputModel? form = null,
        LoginAccountResultViewModel? result = null)
    {
        return new LoginAccountPageViewModel
        {
            Brand = CreateBrand(configuration),
            Form = form ?? new LoginAccountInputModel(),
            Result = result,
            Title = "Iniciar sesion",
            Subtitle = "Accede con la misma cuenta que usas en el launcher y en el cliente del juego.",
            DiscordUrl = configuration["Website:DiscordUrl"] ?? "#",
        };
    }

    public static BrandIdentityViewModel CreateBrand(IConfiguration configuration)
    {
        return new BrandIdentityViewModel
        {
            Name = configuration["Website:BrandName"] ?? "Rollblack Legacy",
            Tagline = configuration["Website:Tagline"] ?? "OneLauncher · Private Server",
            Description = configuration["Website:Description"]
                ?? "Sitio publico de Rollblack Legacy con registro, login y cuenta conectada al auth Sunshine.",
            LogoPath = configuration["Website:LogoPath"] ?? "/images/launcher-branding/logo-title.png",
            FaviconPath = configuration["Website:FaviconPath"] ?? "/images/launcher-branding/favicon.ico",
            Eyebrow = configuration["Website:Eyebrow"] ?? "OneLauncher",
        };
    }
}
