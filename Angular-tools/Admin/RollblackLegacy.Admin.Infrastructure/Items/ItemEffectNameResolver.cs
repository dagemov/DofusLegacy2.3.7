using RollblackLegacy.Admin.Application.Abstractions.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class ItemEffectNameResolver : IItemEffectNameResolver
{
    private readonly AdminProtocolCatalog _protocolCatalog;

    public ItemEffectNameResolver(AdminProtocolCatalog protocolCatalog)
    {
        _protocolCatalog = protocolCatalog;
    }

    public string GetEffectName(int effectId) => _protocolCatalog.GetEffectName(effectId);
}
