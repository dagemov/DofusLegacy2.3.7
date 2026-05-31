namespace RollblackLegacy.Auth.Domain;

public sealed class LegacyAccountSchemaCapabilities
{
    public bool SupportsAccountEmailColumn { get; init; }

    public bool UsesWebsiteContactTable { get; init; }

    public bool EmailWasStored => SupportsAccountEmailColumn || UsesWebsiteContactTable;
}
