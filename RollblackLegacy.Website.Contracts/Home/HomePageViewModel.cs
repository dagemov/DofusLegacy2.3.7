using RollblackLegacy.Website.Contracts.Branding;
using RollblackLegacy.Website.Contracts.Components;

namespace RollblackLegacy.Website.Contracts.Home;

public sealed class HomePageViewModel
{
    public required BrandIdentityViewModel Brand { get; init; }

    public required string HeroBackgroundPath { get; init; }

    public required string HeroBadge { get; init; }

    public required string HeroTitle { get; init; }

    public required string HeroSubtitle { get; init; }

    public required IReadOnlyList<ButtonAtomViewModel> HeroActions { get; init; }

    public required IReadOnlyList<InvitationBulletViewModel> InvitationBullets { get; init; }

    public required PlayJourneyViewModel PlayJourney { get; init; }

    public required IReadOnlyList<NewsItemViewModel> FeatureCards { get; init; }

    public required CommunityCtaViewModel CommunityCta { get; init; }

    public required string BetaStatusLabel { get; init; }
}
