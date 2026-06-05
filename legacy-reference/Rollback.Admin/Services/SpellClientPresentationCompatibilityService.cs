namespace Rollback.Admin.Services;

public sealed class SpellClientPresentationCompatibilityService
{
    private static readonly IReadOnlyDictionary<short, SpellClientPresentationCompatibility> LegacySafePresentations =
        new Dictionary<short, SpellClientPresentationCompatibility>
        {
            // These values come from the historical beta client metadata that casted safely before the 2.10-style publish rewrites.
            [144] = new("animId:801,targetGfxId:20804", 1, 0),
            [145] = new("animId:801,targetGfxId:121", 1, 0),
            [146] = new("animId:802,trailGfxId:124,trailDisplayType:2,trailGfxMinScale:-3,trailGfxMaxScale:+3,targetGfxId:290,useOnlySpellZone:1", 3, 0),
            [147] = new("animId:801,targetGfxId:20804", 1, 0),
            [AdminSpellPresets.MatanzaSpellId] = new("animId:801,targetGfxId:121", 1, 0),
            [AdminSpellPresets.DoomSpellId] = new("animId:802,trailGfxId:124,trailDisplayType:2,trailGfxMinScale:-3,trailGfxMaxScale:+3,targetGfxId:290,useOnlySpellZone:1", 3, 0),
        };

    public bool TryGet(short spellId, out SpellClientPresentationCompatibility compatibility) =>
        LegacySafePresentations.TryGetValue(spellId, out compatibility!);
}

public sealed record SpellClientPresentationCompatibility(
    string ScriptParams,
    int ScriptId,
    int? IconId);
