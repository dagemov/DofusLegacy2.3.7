using RollblackLegacy.Admin.Application.Models;

namespace RollblackLegacy.Admin.Application.Abstractions;

public interface IAdminDatabaseHealthService
{
    Task<AdminDatabaseHealthProbeResult> CheckAsync(CancellationToken cancellationToken = default);
}
