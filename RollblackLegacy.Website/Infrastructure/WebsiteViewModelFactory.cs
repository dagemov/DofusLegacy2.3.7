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
        string launcherUrl = configuration["Website:LauncherDownloadUrl"] ?? "#";
        string adobeAirUrl = configuration["Website:AdobeAirDownloadUrl"] ?? "#";
        string discordUrl = configuration["Website:DiscordUrl"] ?? "#";
        string heroBg = configuration["Website:HeroBackgroundPath"] ?? "/images/branding/Fondo_hero.png";
        string registerImage = configuration["Website:RegisterImagePath"] ?? "/images/branding/Register.png";
        string launcherImage = configuration["Website:LauncherImagePath"] ?? "/images/branding/Launcher.png";
        string serverImage = configuration["Website:ServerImagePath"] ?? "/images/branding/Foto_servidor.png";
        bool launcherExternal = launcherUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        bool airExternal = adobeAirUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        return new HomePageViewModel
        {
            Brand = brand,
            HeroBackgroundPath = heroBg,
            HeroBadge = "Beta abierta",
            HeroTitle = "Dofus 2.0.0 · Servidor privado",
            HeroSubtitle =
                "<strong>Rollblack Legacy</strong> — Servidor privado Dofus 2.0.0 en beta abierta. "
                + "Una comunidad nueva, un equipo apasionado y un mundo que despierta. "
                + "Crea tu cuenta, descarga el launcher y entra al Krosmoz.",
            HeroActions =
            [
                new ButtonAtomViewModel
                {
                    Label = "Crear cuenta",
                    Href = "/account/register",
                    Variant = "primary",
                    Icon = "fa-solid fa-user-plus",
                    Size = "xl",
                    Glow = true,
                },
                new ButtonAtomViewModel
                {
                    Label = "Descargar launcher",
                    Href = launcherUrl,
                    Variant = "secondary",
                    Icon = "fa-solid fa-download",
                    Size = "xl",
                    IsExternal = launcherExternal,
                    Glow = false,
                },
            ],
            BetaStatusLabel = "Beta abierta · Online",
            InvitationBullets =
            [
                new InvitationBulletViewModel
                {
                    Icon = "fa-solid fa-users",
                    IconEmoji = "🌱",
                    Title = "Comunidad nueva",
                    Text = "Forjamos una comunidad desde cero: eventos, feedback directo y un Discord activo donde cada voz cuenta.",
                },
                new InvitationBulletViewModel
                {
                    Icon = "fa-solid fa-hammer",
                    IconEmoji = "⚔️",
                    Title = "Equipo apasionado",
                    Text = "Desarrolladores y jugadores veteranos construyendo un servidor estable, honesto y enfocado en la experiencia.",
                },
                new InvitationBulletViewModel
                {
                    Icon = "fa-solid fa-flask",
                    IconEmoji = "🥚",
                    Title = "Beta Dofus 2.0.0",
                    Text = "Acceso anticipado al cliente 2.0.0 con progresion clasica y economia cuidada desde el dia uno.",
                },
            ],
            PlayJourney = new PlayJourneyViewModel
            {
                Eyebrow = "Comienza tu aventura",
                Title = "Entra a la beta de Rollblack Legacy",
                Subtitle =
                    "Registrate, instala el launcher y explora un mundo inspirado en Dofus 2.0.0 junto a cientos de jugadores.",
                HeaderActions = [],
                Steps =
                [
                    new JourneyStepViewModel
                    {
                        StepNumber = 1,
                        StepLabel = "Paso 1",
                        Title = "Crea tu cuenta",
                        Description = "Utiliza una unica cuenta para acceder a la web, launcher y juego.",
                        Bullets =
                        [
                            "Registro en menos de 1 minuto.",
                            "Misma cuenta para todo el ecosistema.",
                            "Acceso inmediato a la beta.",
                        ],
                        ImagePath = serverImage,
                        ImageAlt = "Servidor Rollblack Legacy",
                        Cta = new ButtonAtomViewModel
                        {
                            Label = "Crear cuenta",
                            Href = "/account/register",
                            Variant = "primary",
                            Icon = "fa-solid fa-user-plus",
                            Glow = true,
                        },
                    },
                    new JourneyStepViewModel
                    {
                        StepNumber = 2,
                        StepLabel = "Paso 2",
                        Title = "Descarga el Launcher Oficial",
                        Description =
                            "Instala el cliente, recibe actualizaciones automaticas y mantente siempre sincronizado con el servidor.",
                        Bullets =
                        [
                            "Instalacion automatica.",
                            "Actualizaciones inteligentes.",
                            "Parches oficiales incluidos.",
                        ],
                        ImagePath = launcherImage,
                        ImageAlt = "Launcher Rollblack Legacy",
                        Cta = new ButtonAtomViewModel
                        {
                            Label = "Descargar Launcher",
                            Href = launcherUrl,
                            Variant = "primary",
                            Icon = "fa-solid fa-download",
                            IsExternal = launcherExternal,
                            Glow = true,
                        },
                    },
                    new JourneyStepViewModel
                    {
                        StepNumber = 3,
                        StepLabel = "Paso 3",
                        Title = "Entra al mundo de Rollblack",
                        Description = "Inicia sesion desde el launcher y comienza tu aventura junto a la comunidad.",
                        Bullets =
                        [
                            "Cliente Dofus 2.0.0.",
                            "Eventos y contenido exclusivo.",
                            "Beta activa y en constante evolucion.",
                        ],
                        ImagePath = serverImage,
                        ImageAlt = "Mundo de Rollblack Legacy",
                    },
                ],
                AdobeAirTitle = "Adobe AIR es obligatorio",
                AdobeAirMessage = "El cliente Dofus 2.0.0 requiere Adobe AIR para ejecutarse correctamente.",
                AdobeAirDownload = new ButtonAtomViewModel
                {
                    Label = "Descargar Adobe AIR",
                    Href = adobeAirUrl,
                    Variant = "ghost",
                    Icon = "fa-solid fa-plug-circle-exclamation",
                    IsExternal = airExternal,
                },
                FinalTitle = "Todo listo para comenzar",
                FinalSubtitle = "Unete a la beta y descubre una nueva experiencia MMORPG.",
                FinalActions =
                [
                    new ButtonAtomViewModel
                    {
                        Label = "Crear cuenta",
                        Href = "/account/register",
                        Variant = "primary",
                        Icon = "fa-solid fa-user-plus",
                        Size = "xl",
                    },
                    new ButtonAtomViewModel
                    {
                        Label = "Descargar launcher",
                        Href = launcherUrl,
                        Variant = "secondary",
                        Icon = "fa-solid fa-download",
                        Size = "xl",
                        IsExternal = launcherExternal,
                    },
                ],
            },
            FeatureCards =
            [
                new NewsItemViewModel
                {
                    Label = "Beta",
                    Title = "Acceso anticipado",
                    Summary = "Fase beta abierta: prueba el servidor, reporta bugs y ayuda a pulir la experiencia.",
                },
                new NewsItemViewModel
                {
                    Label = "Comunidad",
                    Title = "Todos sumamos",
                    Summary = "Discord, registro web y feedback directo con el equipo de desarrollo.",
                },
                new NewsItemViewModel
                {
                    Label = "Legacy",
                    Title = "Espiritu clasico",
                    Summary = "Progresion old-school, combates tacticos y sensacion de MMORPG maduro en 2.0.0.",
                },
            ],
            CommunityCta = new CommunityCtaViewModel
            {
                Title = "Unete a la comunidad de Rollblack Legacy",
                Subtitle =
                    "Habla con el equipo, recibe novedades de la beta y encuentra party en nuestro Discord.",
                DiscordButton = new ButtonAtomViewModel
                {
                    Label = "Unirse a Discord",
                    Href = discordUrl,
                    Variant = "discord",
                    Icon = "fa-brands fa-discord",
                    Size = "xl",
                    IsExternal = true,
                    Glow = false,
                },
            },
        };
    }

    public static RegisterAccountPageViewModel CreateRegisterPage(
        IConfiguration configuration,
        RegisterAccountInputModel? form = null,
        RegisterAccountResultViewModel? result = null)
    {
        string launcherUrl = configuration["Website:LauncherDownloadUrl"] ?? "#";
        string adobeAirUrl = configuration["Website:AdobeAirDownloadUrl"] ?? "#";
        string serverImage = configuration["Website:ServerImagePath"] ?? "/images/branding/Foto_servidor.png";

        return new RegisterAccountPageViewModel
        {
            Brand = CreateBrand(configuration),
            Form = form ?? new RegisterAccountInputModel(),
            Result = result,
            Title = "Crear cuenta",
            Subtitle = string.Empty,
            SecurityHint = string.Empty,
            ServerImagePath = serverImage,
            DiscordUrl = configuration["Website:DiscordUrl"] ?? "#",
            LauncherDownloadUrl = launcherUrl,
            AdobeAirDownloadUrl = adobeAirUrl,
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
            LauncherDownloadUrl = configuration["Website:LauncherDownloadUrl"] ?? "#",
        };
    }

    public static BrandIdentityViewModel CreateBrand(IConfiguration configuration)
    {
        return new BrandIdentityViewModel
        {
            Name = configuration["Website:BrandName"] ?? "Rollblack Legacy",
            Tagline = configuration["Website:Tagline"] ?? "Servidor privado Dofus 2.0.0",
            Description = configuration["Website:Description"]
                ?? "Landing de la beta de Rollblack Legacy: registro, descarga del launcher y comunidad MMORPG.",
            LogoPath = configuration["Website:LogoPath"] ?? "/images/branding/Logo_server.png",
            FaviconPath = configuration["Website:FaviconPath"] ?? "/images/branding/favicon.ico",
            Eyebrow = configuration["Website:Eyebrow"] ?? "Beta MMORPG",
        };
    }
}
