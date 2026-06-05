using System.Buffers.Binary;

namespace RollblackLegacy.Admin.Application.Items;

public sealed record ItemSetBonusEffectLine(int EffectId, int Value, string Format);

public sealed record ItemSetBonusTier(int PieceCount, string TierLabel, IReadOnlyList<ItemSetBonusEffectLine> Effects);

public static class ItemSetEffectsCodec
{
    public static IReadOnlyList<ItemSetBonusTier> DecodeTiers(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return [];
        }

        try
        {
            var data = Convert.FromHexString(hex.Trim());
            if (data.Length < 2)
            {
                return [];
            }

            var offset = 0;
            var tierCount = ReadInt16(data, ref offset);
            var tiers = new List<ItemSetBonusTier>(tierCount);

            for (var tierIndex = 0; tierIndex < tierCount && offset + 2 <= data.Length; tierIndex++)
            {
                var effectCount = ReadInt16(data, ref offset);
                var effects = new List<ItemSetBonusEffectLine>(effectCount);

                for (var effectIndex = 0; effectIndex < effectCount && offset + 4 <= data.Length; effectIndex++)
                {
                    if (offset + 56 > data.Length)
                    {
                        break;
                    }

                    var effectId = (int)ReadUInt32(data, ref offset);
                    var value = ReadInt32(data, ref offset);
                    offset += 4;
                    offset += 4;
                    offset += 4;
                    offset += ReadStringLength(data, ref offset);
                    offset += 4;
                    offset += 4;
                    offset += 4;
                    offset += 1;
                    offset += 12;

                    effects.Add(new ItemSetBonusEffectLine(effectId, value, "Integer"));
                }

                var pieceCount = tierIndex + 2;
                tiers.Add(new ItemSetBonusTier(
                    pieceCount,
                    FormatTierLabel(pieceCount, tierIndex, tierCount),
                    effects));
            }

            return tiers;
        }
        catch
        {
            return [];
        }
    }

    private static string FormatTierLabel(int pieceCount, int tierIndex, int tierCount)
    {
        if (tierIndex == tierCount - 1 && tierCount >= 2)
        {
            return "Set completo";
        }

        return $"{pieceCount} piezas";
    }

    private static int ReadStringLength(byte[] data, ref int offset)
    {
        if (offset + 2 > data.Length)
        {
            return 0;
        }

        var length = ReadInt16(data, ref offset);
        offset += length;
        return 2 + length;
    }

    private static short ReadInt16(byte[] data, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;
        return value;
    }

    private static int ReadInt32(byte[] data, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));
        offset += 4;
        return value;
    }

    private static uint ReadUInt32(byte[] data, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
        offset += 4;
        return value;
    }
}
