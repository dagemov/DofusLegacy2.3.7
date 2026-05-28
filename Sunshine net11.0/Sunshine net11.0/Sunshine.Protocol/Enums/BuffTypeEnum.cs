using System;
namespace Sunshine.Protocol.Enums
{
	public enum BuffTypeEnum
	{
		BUFF_ADDED = 1,
		TURN_BEGIN = 2,
		TURN_END = 4,
		MOVE = 8,
		BEFORE_ATTACKED = 16,
		AFTER_ATTACKED = 32,
		BEFORE_ATTACK = 64,
		AFTER_ATTACK = 128,
		BUFF_ENDED = 256,
		BUFF_ENDED_TURNEND = 512,
		UNKNOWN = 2147483647
	}
}
