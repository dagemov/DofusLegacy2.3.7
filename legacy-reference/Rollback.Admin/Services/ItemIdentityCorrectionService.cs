using MySqlConnector;
using Rollback.Admin.Infrastructure;
using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Items;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class ItemIdentityCorrectionService
{
    private readonly AdminDbConnectionFactory _connectionFactory;
    private readonly ItemIdentityDiagnosticService _diagnosticService;

    public ItemIdentityCorrectionService(
        AdminDbConnectionFactory connectionFactory,
        ItemIdentityDiagnosticService diagnosticService)
    {
        _connectionFactory = connectionFactory;
        _diagnosticService = diagnosticService;
    }

    public async Task<ItemIdentityCorrectionPlan> PreviewAsync(short itemId, CancellationToken cancellationToken = default)
    {
        var report = await _diagnosticService.DiagnoseAsync(itemId, cancellationToken);
        if (report.Runtime is null)
        {
            return new ItemIdentityCorrectionPlan
            {
                ItemId = itemId,
                CanApply = false,
                Summary = "No hay template runtime para corregir.",
                Warnings = new[] { "Este ItemId no existe en items_templates. Primero tendrias que crearlo o revisar si el id es correcto." },
            };
        }

        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        return await BuildPlanAsync(connection, report, cancellationToken);
    }

    public async Task<AdminSaveResult> ApplyAsync(short itemId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateWorldConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var report = await _diagnosticService.DiagnoseAsync(itemId, cancellationToken);
            var plan = await BuildPlanAsync(connection, report, cancellationToken);
            if (!plan.CanApply || report.Runtime is null)
            {
                return plan.Warnings.Count == 0
                    ? new AdminSaveResult { Warnings = new[] { "No habia correcciones puntuales aplicables para este item." } }
                    : new AdminSaveResult { Warnings = plan.Warnings };
            }

            var targetTypeId = plan.CorrectedTypeId ?? report.Runtime.TypeId;
            var targetItemSetId = plan.CorrectedItemSetId ?? report.Runtime.ItemSetId;
            var targetAppearanceId = plan.CorrectedAppearanceId ?? report.Runtime.AppearanceId;

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE items_templates
                    SET TypeId = @typeId,
                        ItemSetId = @itemSetId,
                        AppearanceId = @appearanceId
                    WHERE Id = @itemId;
                    """;
                command.Parameters.AddWithValue("@itemId", itemId);
                command.Parameters.AddWithValue("@typeId", (short)targetTypeId);
                command.Parameters.AddWithValue("@itemSetId", targetItemSetId);
                command.Parameters.AddWithValue("@appearanceId", targetAppearanceId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await SyncSetMembershipAsync(connection, transaction, itemId, targetItemSetId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(plan.SuggestedOverrideName) || !string.IsNullOrWhiteSpace(plan.SuggestedOverrideDescription))
            {
                await AdminEntityTextOverrideService.SaveAsync(
                    connection,
                    AdminEntityType.Item,
                    itemId,
                    plan.SuggestedOverrideName,
                    plan.SuggestedOverrideDescription,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return plan.Warnings.Count == 0
                ? AdminSaveResult.Empty
                : new AdminSaveResult { Warnings = plan.Warnings };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ItemIdentityCorrectionPlan> BuildPlanAsync(
        MySqlConnection connection,
        ItemDiagnosticReport report,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var changes = new List<ItemIdentityCorrectionChange>();
        ItemType? correctedTypeId = null;
        short? correctedItemSetId = null;
        short? correctedAppearanceId = null;
        string? suggestedOverrideName = null;
        string? suggestedOverrideDescription = null;

        if (report.Runtime is null)
        {
            return new ItemIdentityCorrectionPlan
            {
                ItemId = report.ItemId,
                CanApply = false,
                Summary = "No hay runtime que corregir.",
                Warnings = new[] { "El item no existe en runtime." },
            };
        }

        if (!report.Audit.HasClientMetadata)
        {
            warnings.Add("Esta correccion no hara visible en el cliente un ItemId que el datacenter actual no conoce. Solo alinea metadata runtime y administrativa.");
        }

        if (report.Reference is not null && report.Runtime.TypeId != (ItemType)report.Reference.TypeId)
        {
            if (report.Client.ClientTypeId.HasValue &&
                (short)report.Client.ClientTypeId.Value != report.Reference.TypeId)
            {
                warnings.Add($"La referencia propone TypeId {report.Reference.TypeId}, pero el cliente actual resuelve {(short)report.Client.ClientTypeId.Value}. Se conserva runtime/cliente y no se aplica ese cambio.");
            }
            else
            {
                correctedTypeId = (ItemType)report.Reference.TypeId;
                changes.Add(new ItemIdentityCorrectionChange
                {
                    Field = "TypeId",
                    CurrentValue = $"{(short)report.Runtime.TypeId} ({ItemTypeLabelService.GetDisplayName(report.Runtime.TypeId)})",
                    SuggestedValue = $"{report.Reference.TypeId} ({report.Reference.TypeLabel})",
                    SourceLabel = "Referencia",
                });
            }
        }
        else if (report.Reference is null &&
                 report.Client.ClientTypeId.HasValue &&
                 report.Runtime.TypeId != report.Client.ClientTypeId.Value)
        {
            correctedTypeId = report.Client.ClientTypeId.Value;
            changes.Add(new ItemIdentityCorrectionChange
            {
                Field = "TypeId",
                CurrentValue = $"{(short)report.Runtime.TypeId} ({ItemTypeLabelService.GetDisplayName(report.Runtime.TypeId)})",
                SuggestedValue = $"{(short)report.Client.ClientTypeId.Value} ({ItemTypeLabelService.GetDisplayName(report.Client.ClientTypeId.Value)})",
                SourceLabel = "Cliente",
            });
        }

        if (report.Reference is not null && report.Reference.AppearanceId > 0 && report.Runtime.AppearanceId != report.Reference.AppearanceId)
        {
            if (report.Client.ClientAppearanceId is > 0 &&
                report.Client.ClientAppearanceId.Value != report.Reference.AppearanceId)
            {
                warnings.Add($"La referencia propone AppearanceId {report.Reference.AppearanceId}, pero el cliente actual resuelve {report.Client.ClientAppearanceId.Value}. Se conserva runtime/cliente y no se aplica ese cambio.");
            }
            else
            {
                correctedAppearanceId = (short)Math.Clamp(report.Reference.AppearanceId, short.MinValue, short.MaxValue);
                changes.Add(new ItemIdentityCorrectionChange
                {
                    Field = "AppearanceId",
                    CurrentValue = report.Runtime.AppearanceId.ToString(),
                    SuggestedValue = report.Reference.AppearanceId.ToString(),
                    SourceLabel = "Referencia",
                });
            }
        }
        else if (report.Reference is null &&
                 report.Client.ClientAppearanceId is > 0 &&
                 report.Runtime.AppearanceId != report.Client.ClientAppearanceId.Value)
        {
            correctedAppearanceId = report.Client.ClientAppearanceId.Value;
            changes.Add(new ItemIdentityCorrectionChange
            {
                Field = "AppearanceId",
                CurrentValue = report.Runtime.AppearanceId.ToString(),
                SuggestedValue = report.Client.ClientAppearanceId.Value.ToString(),
                SourceLabel = "Cliente",
            });
        }

        if (report.Reference is { ItemSetId: > 0 } &&
            report.Runtime.ItemSetId != report.Reference.ItemSetId)
        {
            if (await RuntimeSetExistsAsync(connection, report.Reference.ItemSetId, cancellationToken))
            {
                correctedItemSetId = report.Reference.ItemSetId;
                changes.Add(new ItemIdentityCorrectionChange
                {
                    Field = "ItemSetId",
                    CurrentValue = report.Runtime.ItemSetId.ToString(),
                    SuggestedValue = $"{report.Reference.ItemSetId} ({(string.IsNullOrWhiteSpace(report.ReferenceSet?.Name) ? $"Set #{report.Reference.ItemSetId}" : report.ReferenceSet!.Name)})",
                    SourceLabel = "Referencia",
                });
            }
            else
            {
                warnings.Add($"La referencia sugiere ItemSetId {report.Reference.ItemSetId}, pero ese set no existe en runtime. No se aplico el cambio automaticamente.");
            }
        }

        if (string.IsNullOrWhiteSpace(report.OverrideName) &&
            string.IsNullOrWhiteSpace(report.Client.Name) &&
            !string.IsNullOrWhiteSpace(report.Reference?.Name))
        {
            suggestedOverrideName = report.Reference.Name.Trim();
            changes.Add(new ItemIdentityCorrectionChange
            {
                Field = "OverrideName",
                CurrentValue = "-",
                SuggestedValue = suggestedOverrideName,
                SourceLabel = "Referencia",
            });
        }

        if (string.IsNullOrWhiteSpace(report.OverrideDescription) &&
            string.IsNullOrWhiteSpace(report.Client.Description) &&
            !string.IsNullOrWhiteSpace(report.Reference?.Description))
        {
            suggestedOverrideDescription = report.Reference.Description.Trim();
            changes.Add(new ItemIdentityCorrectionChange
            {
                Field = "OverrideDescription",
                CurrentValue = "-",
                SuggestedValue = Truncate(report.Reference.Description.Trim(), 96),
                SourceLabel = "Referencia",
            });
        }

        var canApply = changes.Count > 0;
        return new ItemIdentityCorrectionPlan
        {
            ItemId = report.ItemId,
            CanApply = canApply,
            Summary = canApply
                ? "Se detectaron correcciones puntuales seguras sobre metadata de identidad. No se tocaran BinaryEffects ni stats jugables."
                : "No hay correcciones puntuales seguras para aplicar en este item.",
            Warnings = warnings,
            Changes = changes,
            CorrectedTypeId = correctedTypeId,
            CorrectedItemSetId = correctedItemSetId,
            CorrectedAppearanceId = correctedAppearanceId,
            SuggestedOverrideName = suggestedOverrideName,
            SuggestedOverrideDescription = suggestedOverrideDescription,
        };
    }

    private static async Task<bool> RuntimeSetExistsAsync(MySqlConnection connection, short setId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM items_sets WHERE Id = @setId LIMIT 1;";
        command.Parameters.AddWithValue("@setId", setId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null and not DBNull;
    }

    private static async Task SyncSetMembershipAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        short itemId,
        short targetSetId,
        CancellationToken cancellationToken)
    {
        var setRows = new List<(short Id, string ItemsCsv)>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT Id, ItemsCSV FROM items_sets WHERE FIND_IN_SET(@itemId, ItemsCSV) > 0 OR Id = @targetSetId;";
            command.Parameters.AddWithValue("@itemId", itemId);
            command.Parameters.AddWithValue("@targetSetId", targetSetId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                setRows.Add((reader.GetSafeInt16("Id"), reader.GetSafeString("ItemsCSV")));
        }

        foreach (var row in setRows)
        {
            var itemIds = row.ItemsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => short.TryParse(x, out _))
                .Select(short.Parse)
                .Where(x => x > 0 && x != itemId)
                .ToList();

            if (row.Id == targetSetId && targetSetId > 0)
                itemIds.Add(itemId);

            var normalizedCsv = string.Join(",", itemIds.Distinct().OrderBy(x => x));
            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE items_sets SET ItemsCSV = @itemsCsv WHERE Id = @setId;";
            update.Parameters.AddWithValue("@setId", row.Id);
            update.Parameters.AddWithValue("@itemsCsv", normalizedCsv);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
}
