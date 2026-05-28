using System;

namespace Sunshine.Protocol.Enums
{
    [Flags]
    public enum HouseRightsBitEnum : uint
    {
        NONE = 0,
        HOUSE_SHARE_GUILD = 1,
        HOUSE_GUILD_DOORS_LIST_GUILD = 2,
        HOUSE_GUILD_DOORS_LIST_OTHERS = 4,
        HOUSE_ALLOW_GUILD_MEMBER_ACCESS = 8,
        HOUSE_FORBID_OTHERS_ACCESS = 16,
        HOUSE_ALLOW_GUILD_MEMBER_ACCESS_CHEST = 32,
        HOUSE_FORBID_OTHERS_ACCESS_CHEST = 64,
        HOUSE_ALLOW_GUILD_MEMBER_REST = 128,
        HOUSE_ALLOW_GUILD_MEMBER_TELEPORT = 256
    }
}
