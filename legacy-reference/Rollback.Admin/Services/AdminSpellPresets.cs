using Rollback.Protocol.Enums;
using Rollback.World.Game.Spells;

namespace Rollback.Admin.Services;

public static class AdminSpellPresets
{
    public const short MatanzaSpellId = StaffSpecialSpellPolicy.MatanzaSpellId;
    public const short DoomSpellId = StaffSpecialSpellPolicy.DoomSpellId;
    public const short RollBackSpellId = StaffSpecialSpellPolicy.RollBackSpellId;

    private static readonly IReadOnlyList<int> AllBreeds =
        Enumerable.Range((int)BreedEnum.Feca, (int)BreedEnum.Pandawa - (int)BreedEnum.Feca + 1).ToArray();

    public static IReadOnlyList<AdminSpecialSpellDefinition> All { get; } =
        new[]
        {
            new AdminSpecialSpellDefinition(
                MatanzaSpellId,
                "Matanza",
                "Golpea 999 de cada elemento.",
                145,
                61,
                false,
                false,
                Array.Empty<int>()),
            new AdminSpecialSpellDefinition(
                DoomSpellId,
                "Doom",
                "Golpea 5000 neutro para QA.",
                146,
                62,
                false,
                false,
                Array.Empty<int>()),
            new AdminSpecialSpellDefinition(
                RollBackSpellId,
                "Spell RollBack",
                "Golpea 999 neutro, tierra, fuego, agua y aire para QA/admin.",
                146,
                60,
                true,
                true,
                AllBreeds),
        };

    public static bool TryGet(short spellId, out AdminSpecialSpellDefinition definition)
    {
        spellId = StaffSpecialSpellPolicy.NormalizeAssignedSpellId(spellId);
        definition = All.FirstOrDefault(x => x.SpellId == spellId)!;
        return definition is not null;
    }
}

public sealed record AdminSpecialSpellDefinition(
    short SpellId,
    string Name,
    string Description,
    short VisualReferenceSpellId,
    byte PreferredPosition,
    bool IsGrimoireSafe,
    bool CanAssignFromAdmin,
    IReadOnlyList<int> AssignedBreedIds)
{
    public string AssignmentModeLabel =>
        !CanAssignFromAdmin
            ? "Congelado"
            : IsGrimoireSafe
                ? "Staff/admin visible"
                : "Staff/admin seguro";

    public string ClientCompatibilityLabel =>
        !CanAssignFromAdmin
            ? "Congelado por incompatibilidad UI cliente pendiente de resolver"
            : IsGrimoireSafe
                ? "Visible en grimoire con metadata cliente y mapping de raza controlado"
                : "No entra al grimoire normal: este cliente lo rompe si falta mapping de raza";
}
