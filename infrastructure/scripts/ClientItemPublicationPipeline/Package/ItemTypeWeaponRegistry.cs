using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record WeaponTypeExclusion(
    int TypeId,
    string Reason,
    string? TypeNameEs,
    string? TypeNameEn,
    string? EnumName);

internal sealed class ItemTypeWeaponRegistry
{
    private static readonly string[] WeaponKeywords =
    [
        "espada", "sword", "epee", "épée",
        "daga", "dagger", "dague",
        "arco", "bow", "arc",
        "pala", "shovel", "pelle",
        "martillo", "hammer", "marteau",
        "baston", "bastón", "staff", "baton", "bâton",
        "varita", "wand", "baguette",
        "hacha", "axe", "hache",
        "weapon", "arma", "arme",
        "arbalète", "arbalete", "arbalet",
        "faux", "scythe", "pioche", "pickaxe", "outil", "tool"
    ];

    public IReadOnlySet<int> ExcludedTypeIds { get; }
    public IReadOnlyList<WeaponTypeExclusion> Exclusions { get; }

    private ItemTypeWeaponRegistry(IReadOnlySet<int> excludedTypeIds, IReadOnlyList<WeaponTypeExclusion> exclusions)
    {
        ExcludedTypeIds = excludedTypeIds;
        Exclusions = exclusions;
    }

    public bool IsWeapon(int typeId) => ExcludedTypeIds.Contains(typeId);

    public static ItemTypeWeaponRegistry Build(string? itemTypesPath, D2i.D2iFile? es, D2i.D2iFile? en)
    {
        var excluded = new HashSet<int>(WeaponTypeFilter.WeaponTypeIds);
        var details = new List<WeaponTypeExclusion>();

        foreach (var typeId in WeaponTypeFilter.WeaponTypeIds)
        {
            details.Add(new WeaponTypeExclusion(
                typeId,
                "static-weapon-type-id",
                null,
                null,
                Enum.IsDefined(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId)
                    ? Enum.GetName(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId)
                    : null));
        }

        foreach (var value in Enum.GetValues<Sunshine.Protocol.Enums.ItemTypeEnum>())
        {
            var typeId = (int)value;
            var enumName = value.ToString();
            if (MatchesWeaponKeyword(enumName))
            {
                if (excluded.Add(typeId))
                {
                    details.Add(new WeaponTypeExclusion(typeId, "enum-keyword", null, null, enumName));
                }
            }
        }

        if (File.Exists(itemTypesPath) && es is not null && en is not null)
        {
            AppendFromItemTypesIndex(itemTypesPath, es, en, excluded, details);
        }

        return new ItemTypeWeaponRegistry(excluded, details.OrderBy(static d => d.TypeId).ToList());
    }

    private static void AppendFromItemTypesIndex(
        string itemTypesPath,
        D2i.D2iFile es,
        D2i.D2iFile en,
        HashSet<int> excluded,
        List<WeaponTypeExclusion> details)
    {
        // ItemTypes.d2o: solo índice + heurística i18n vía nameIds referenciados en Items (sin clase D2O registrada).
        var typeIds = ClientPatchD2oIndex.ReadIds(itemTypesPath);
        foreach (var typeId in typeIds)
        {
            if (excluded.Contains(typeId))
            {
                continue;
            }

            var enumName = Enum.IsDefined(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId)
                ? Enum.GetName(typeof(Sunshine.Protocol.Enums.ItemTypeEnum), typeId)
                : null;

            if (MatchesWeaponKeyword(enumName) && excluded.Add(typeId))
            {
                details.Add(new WeaponTypeExclusion(typeId, "enum-from-index", null, null, enumName));
            }
        }
    }

    public void WriteExclusionReport(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var jsonPath = Path.Combine(outputDirectory, "weapon-type-exclusions.json");
        var mdPath = Path.Combine(outputDirectory, "weapon-type-exclusions.md");

        var payload = new
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            ExcludedTypeIds = ExcludedTypeIds.OrderBy(static id => id).ToList(),
            Exclusions
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);

        var builder = new StringBuilder();
        builder.AppendLine("# Weapon TypeIds excluidos");
        builder.AppendLine();
        builder.AppendLine($"Total: **{ExcludedTypeIds.Count}**");
        builder.AppendLine();
        foreach (var row in Exclusions)
        {
            builder.AppendLine(
                $"- `{row.TypeId}` — {row.Reason} — ES: {row.TypeNameEs ?? "-"} — EN: {row.TypeNameEn ?? "-"} — enum: {row.EnumName ?? "-"}");
        }

        File.WriteAllText(mdPath, builder.ToString(), Encoding.UTF8);
    }

    private static bool MatchesWeaponKeyword(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.ToLowerInvariant().Replace('_', ' ');
        return WeaponKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal));
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
