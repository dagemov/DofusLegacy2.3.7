namespace RollblackLegacy.Website.Contracts.Branding;

public sealed class BrandIdentityViewModel
{
    public required string Name { get; init; }

    public required string Tagline { get; init; }

    public required string Description { get; init; }

    public required string LogoPath { get; init; }

    public required string FaviconPath { get; init; }
}
