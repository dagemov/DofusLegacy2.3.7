using RollblackLegacy.Admin.Contracts.Publication;

namespace RollblackLegacy.Admin.Application.Abstractions.Publication;

public interface IPublicationBackupStatusService
{
    Task<PublicationBackupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
