using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RollblackLegacy.Admin.Application.Items;

public sealed record ItemSetBonusEffectLine(
    int EffectId,
    int Value,
    int? DiceNum,
    int? DiceSide,
    string Format);

public sealed record ItemSetBonusTier(int PieceCount, string TierLabel, IReadOnlyList<ItemSetBonusEffectLine> Effects);

public static class ItemSetEffectsCodec
{
    public const string EmptyEffectsHex = "0000";

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

                    var effectStart = offset;
                    var effectId = (int)ReadUInt32(data, ref offset);
                    var value = ReadInt32(data, ref offset);
                    var diceNum = ReadInt32(data, ref offset);
                    var diceSide = ReadInt32(data, ref offset);
                    offset += 4;
                    offset += ReadStringLength(data, ref offset);
                    offset += 4;
                    offset += 4;
                    offset += 4;
                    offset += 1;
                    offset += 12;

                    var consumed = offset - effectStart;
                    if (consumed < 56)
                    {
                        offset = effectStart + 56;
                    }

                    var format = diceNum > 0 || diceSide > 0 ? "Dice" : "Integer";
                    effects.Add(new ItemSetBonusEffectLine(
                        effectId,
                        value,
                        diceNum > 0 ? diceNum : null,
                        diceSide > 0 ? diceSide : null,
                        format));
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

    public static string EncodeTiers(IReadOnlyList<ItemSetBonusTierWriteInput> tiers)
    {
        var normalized = tiers
            .Where(tier => tier.PieceCount >= 2)
            .OrderBy(tier => tier.PieceCount)
            .ToList();

        if (normalized.Count == 0)
        {
            return EmptyEffectsHex;
        }

        var buffer = new List<byte>(capacity: 128);
        WriteInt16(buffer, (short)normalized.Count);

        foreach (var tier in normalized)
        {
            var effects = tier.Effects
                .Where(effect => effect.EffectId > 0)
                .ToList();

            WriteInt16(buffer, (short)effects.Count);

            foreach (var effect in effects)
            {
                WriteEffect(buffer, effect);
            }
        }

        return Convert.ToHexString(CollectionsMarshal.AsSpan(buffer));
    }

    public static IReadOnlyList<ItemSetBonusTier> ApplyPieceCounts(
        IReadOnlyList<ItemSetBonusTierWriteInput> tiers,
        IReadOnlyList<ItemSetBonusTier> decoded)
    {
        if (tiers.Count == 0)
        {
            return decoded;
        }

        var decodedByIndex = decoded.ToList();
        return tiers
            .OrderBy(tier => tier.PieceCount)
            .Select((tier, index) =>
            {
                var effects = tier.Effects
                    .Where(effect => effect.EffectId > 0)
                    .Select(effect => new ItemSetBonusEffectLine(
                        effect.EffectId,
                        effect.Value,
                        effect.DiceNum,
                        effect.DiceSide,
                        effect.Format))
                    .ToList();

                return new ItemSetBonusTier(
                    tier.PieceCount,
                    FormatTierLabel(tier.PieceCount, index, tiers.Count),
                    effects);
            })
            .ToList();
    }

    private static void WriteEffect(List<byte> buffer, ItemSetBonusEffectWriteInput effect)
    {
        var start = buffer.Count;
        WriteUInt32(buffer, (uint)effect.EffectId);
        WriteInt32(buffer, effect.Value);

        var diceNum = string.Equals(effect.Format, "Dice", StringComparison.OrdinalIgnoreCase)
            ? effect.DiceNum ?? 0
            : 0;
        var diceSide = string.Equals(effect.Format, "Dice", StringComparison.OrdinalIgnoreCase)
            ? effect.DiceSide ?? 0
            : 0;

        WriteInt32(buffer, diceNum);
        WriteInt32(buffer, diceSide);
        WriteInt32(buffer, 0);
        WriteInt16(buffer, 0);
        WriteInt32(buffer, 0);
        WriteInt32(buffer, 0);
        WriteInt32(buffer, 0);
        buffer.Add(0);

        for (var i = 0; i < 12; i++)
        {
            buffer.Add(0);
        }

        while (buffer.Count - start < 56)
        {
            buffer.Add(0);
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

    private static void WriteInt16(List<byte> buffer, short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, value);
        buffer.AddRange(bytes);
    }

    private static void WriteInt32(List<byte> buffer, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        buffer.AddRange(bytes);
    }

    private static void WriteUInt32(List<byte> buffer, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        buffer.AddRange(bytes);
    }
}

public sealed record ItemSetBonusEffectWriteInput(
    int EffectId,
    int Value,
    int? DiceNum,
    int? DiceSide,
    string Format);

public sealed record ItemSetBonusTierWriteInput(
    int PieceCount,
    IReadOnlyList<ItemSetBonusEffectWriteInput> Effects);
