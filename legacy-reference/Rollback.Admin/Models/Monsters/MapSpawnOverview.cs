namespace Rollback.Admin.Models.Monsters;

public sealed class MapSpawnOverview
{
    public int MapId { get; set; }

    public short SubAreaId { get; set; }

    public sbyte X { get; set; }

    public sbyte Y { get; set; }

    public bool SpawnDisabled { get; set; }

    public int DirectSpawnCount { get; set; }

    public int SubAreaSpawnCount { get; set; }

    public int EffectiveSpawnCount => DirectSpawnCount + SubAreaSpawnCount;

    public bool IsEmpty => EffectiveSpawnCount == 0;
}
