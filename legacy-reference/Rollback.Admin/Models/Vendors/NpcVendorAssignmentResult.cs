namespace Rollback.Admin.Models.Vendors;

public sealed class NpcVendorAssignmentResult
{
    public int EffectiveShopActionId { get; init; }

    public short EffectiveNpcId { get; init; }

    public string EffectiveVendorName { get; init; } = string.Empty;

    public bool Redirected { get; init; }

    public string Message { get; init; } = string.Empty;
}
