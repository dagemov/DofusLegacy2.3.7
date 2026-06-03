namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterGradeAdminModel
{
    public sbyte Grade { get; set; } = 1;

    public short Level { get; set; } = 1;

    public int Health { get; set; } = 10;

    public short AP { get; set; } = 6;

    public short MP { get; set; } = 3;

    public short APDodge { get; set; }

    public short MPDodge { get; set; }

    public short EarthResistance { get; set; }

    public short AirResistance { get; set; }

    public short FireResistance { get; set; }

    public short WaterResistance { get; set; }

    public short NeutralResistance { get; set; }

    public short Wisdom { get; set; }

    public short Strength { get; set; }

    public short Intelligence { get; set; }

    public short Chance { get; set; }

    public short Agility { get; set; }

    public long Experience { get; set; }

    public int MinKamas { get; set; }

    public int MaxKamas { get; set; }
}
