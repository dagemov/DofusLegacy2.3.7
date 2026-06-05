using Rollback.Admin.Models.Common;
using Rollback.Admin.Models.Spells;

namespace Rollback.Admin.Services;

public sealed class SpellPublishOrchestrator
{
    private readonly SpellAdminService _spellAdminService;
    private readonly SpellClientPublishService _spellClientPublishService;

    public SpellPublishOrchestrator(
        SpellAdminService spellAdminService,
        SpellClientPublishService spellClientPublishService)
    {
        _spellAdminService = spellAdminService;
        _spellClientPublishService = spellClientPublishService;
    }

    public async Task<AdminSaveResult> SaveAndPublishAsync(
        SpellEditModel model,
        CancellationToken cancellationToken = default)
    {
        var runtimeSaveResult = await _spellAdminService.SaveRuntimeAsync(model, cancellationToken);
        var clientPublishResult = await _spellClientPublishService.PublishAsync(model, cancellationToken);

        return new AdminSaveResult
        {
            Infos = string.IsNullOrWhiteSpace(clientPublishResult.Summary)
                ? runtimeSaveResult.Infos
                : runtimeSaveResult.Infos.Concat(new[] { clientPublishResult.Summary }).ToArray(),
            Warnings = runtimeSaveResult.Warnings.Concat(clientPublishResult.Warnings).ToArray(),
            Errors = runtimeSaveResult.Errors,
        };
    }
}
