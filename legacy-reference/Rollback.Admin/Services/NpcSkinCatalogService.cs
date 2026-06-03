using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Npcs;

namespace Rollback.Admin.Services;

public sealed class NpcSkinCatalogService
{
    private readonly AdminDbConnectionFactory _connectionFactory;

    public NpcSkinCatalogService(AdminDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<NpcSkinOption>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);

        var normalizedSearch = search?.Trim() ?? string.Empty;
        var wildSearch = $"%{normalizedSearch}%";

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT *
            FROM (
                SELECT
                    nt.Id AS SampleId,
                    nt.Name AS DisplayName,
                    nt.EntityLookString,
                    nt.Gender,
                    'NPC' AS SourceLabel
                FROM npcs_templates nt
                WHERE @search = ''
                   OR CAST(nt.Id AS CHAR) LIKE @wildSearch
                   OR nt.Name LIKE @wildSearch
                   OR nt.EntityLookString LIKE @wildSearch

                UNION ALL

                SELECT
                    mt.Id AS SampleId,
                    CONCAT('Monster #', mt.Id) AS DisplayName,
                    mt.EntityLookString,
                    0 AS Gender,
                    'Monstruo' AS SourceLabel
                FROM monsters_templates mt
                WHERE @search = ''
                   OR CAST(mt.Id AS CHAR) LIKE @wildSearch
                   OR mt.EntityLookString LIKE @wildSearch
            ) options
            WHERE options.EntityLookString IS NOT NULL
              AND TRIM(options.EntityLookString) <> ''
            ORDER BY options.SourceLabel, options.SampleId
            LIMIT 40;
            """;
        command.Parameters.AddWithValue("@search", normalizedSearch);
        command.Parameters.AddWithValue("@wildSearch", wildSearch);

        var result = new List<NpcSkinOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new NpcSkinOption
            {
                SampleId = reader.GetSafeInt16("SampleId"),
                DisplayName = reader.GetSafeString("DisplayName"),
                EntityLookString = reader.GetSafeString("EntityLookString"),
                Gender = reader.GetSafeBoolean("Gender"),
                SourceLabel = reader.GetSafeString("SourceLabel"),
            });
        }

        return result;
    }
}
