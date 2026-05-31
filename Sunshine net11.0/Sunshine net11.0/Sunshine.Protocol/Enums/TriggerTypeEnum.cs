using System;
namespace Sunshine.Protocol.Enums
{
    [Flags]
    public enum TriggerTypeEnum
    {
        NEVER = 0,
        TURN_BEGIN = 1,
        TURN_END = 2,
        MOVE = 4,
        CREATION = 8
    }
}
