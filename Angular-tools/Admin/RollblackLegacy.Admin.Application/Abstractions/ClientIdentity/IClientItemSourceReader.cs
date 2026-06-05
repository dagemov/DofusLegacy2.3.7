using RollblackLegacy.Admin.Application.Models.ClientIdentity;

namespace RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;

public interface IClientItemSourceReader
{
    Task<ClientItemSourceSnapshot> ReadAsync(ClientItemDbSnapshot dbItem, CancellationToken cancellationToken = default);
}
