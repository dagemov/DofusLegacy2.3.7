using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class SunshineItemEffectsCodec
{
    public const string EmptyEffectsHex = "0000";

    public const short TypeInteger = 70;
    public const short TypeCreature = 71;
    public const short TypeDice = 73;
    public const short TypeDurationLegacy = 74;
    public const short TypeDuration = 75;
    public const short TypeBase = 76;
    public const short TypeMinMax = 82;

    public SunshineItemEffectsDecodeResult Decode(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return new SunshineItemEffectsDecodeResult([], null, []);
        }

        try
        {
            var data = Convert.FromHexString(hex.Trim());
            if (data.Length < 2)
            {
                return new SunshineItemEffectsDecodeResult([], null, []);
            }

            var offset = 0;
            var declaredCount = ReadInt16(data, ref offset);
            var entries = new List<SunshineEffectEntry>(declaredCount);
            var warnings = new List<string>();

            for (var index = 0; index < declaredCount && offset + 4 <= data.Length; index++)
            {
                var effectStart = offset;
                var typeId = ReadInt16(data, ref offset);
                var actionId = ReadInt16(data, ref offset);

                if (!TryReadSupportedPayload(data, ref offset, typeId, actionId, out var entry))
                {
                    var preservedHex = Convert.ToHexString(data.AsSpan(effectStart));
                    entries.Add(new SunshineEffectEntry(
                        typeId,
                        actionId,
                        DiceNum: 0,
                        DiceSide: 0,
                        Value: 0,
                        MinValue: 0,
                        MaxValue: 0,
                        IsSupported: false,
                        PreservedEffectHex: preservedHex));

                    warnings.Add(
                        $"Effect type {typeId} (action {actionId}) is not editable in Phase 7B. Remaining bytes were preserved verbatim.");

                    var suffix = offset < data.Length
                        ? Convert.ToHexString(data.AsSpan(offset))
                        : null;

                    return new SunshineItemEffectsDecodeResult(entries, suffix, warnings);
                }

                entries.Add(entry);
            }

            return new SunshineItemEffectsDecodeResult(entries, null, warnings);
        }
        catch (Exception ex)
        {
            return new SunshineItemEffectsDecodeResult(
                [],
                null,
                [$"Effects payload could not be decoded safely: {ex.Message}"]);
        }
    }

    public string Encode(
        IReadOnlyList<SunshineEffectEntry> effects,
        string? preservedSuffixHex)
    {
        var supported = effects
            .Where(x => x.IsSupported)
            .ToList();

        var buffer = new List<byte>(capacity: 64);
        WriteInt16(buffer, (short)supported.Count);

        foreach (var entry in supported)
        {
            WriteSupportedEntry(buffer, entry);
        }

        if (!string.IsNullOrWhiteSpace(preservedSuffixHex))
        {
            buffer.AddRange(Convert.FromHexString(preservedSuffixHex.Trim()));
        }

        if (buffer.Count == 0)
        {
            return EmptyEffectsHex;
        }

        return Convert.ToHexString(CollectionsMarshal.AsSpan(buffer));
    }

    private static bool TryReadSupportedPayload(
        byte[] data,
        ref int offset,
        short typeId,
        int actionId,
        out SunshineEffectEntry entry)
    {
        entry = null!;

        switch (typeId)
        {
            case TypeInteger:
            {
                var value = ReadInt16Safe(data, ref offset);
                entry = new SunshineEffectEntry(typeId, actionId, 0, 0, value, 0, 0, true, null);
                return true;
            }
            case TypeDice:
            {
                var diceNum = ReadInt16Safe(data, ref offset);
                var diceSide = ReadInt16Safe(data, ref offset);
                var diceConst = ReadInt16Safe(data, ref offset);
                entry = new SunshineEffectEntry(typeId, actionId, diceNum, diceSide, diceConst, 0, 0, true, null);
                return true;
            }
            case TypeMinMax:
            {
                var min = ReadInt16Safe(data, ref offset);
                var max = ReadInt16Safe(data, ref offset);
                entry = new SunshineEffectEntry(typeId, actionId, 0, 0, min, min, max, true, null);
                return true;
            }
            case TypeCreature:
            {
                var familyId = ReadInt16Safe(data, ref offset);
                entry = new SunshineEffectEntry(typeId, actionId, 0, 0, familyId, 0, 0, true, null);
                return true;
            }
            case TypeDuration:
            case TypeDurationLegacy:
            {
                var days = ReadInt16Safe(data, ref offset);
                var hours = ReadInt16Safe(data, ref offset);
                var minutes = ReadInt16Safe(data, ref offset);
                entry = new SunshineEffectEntry(typeId, actionId, days, hours, minutes, 0, 0, true, null);
                return true;
            }
            case TypeBase:
            {
                entry = new SunshineEffectEntry(typeId, actionId, 0, 0, 0, 0, 0, true, null);
                return true;
            }
            default:
                return false;
        }
    }

    private static void WriteSupportedEntry(List<byte> buffer, SunshineEffectEntry entry)
    {
        var typeId = entry.SerializationTypeId == TypeDurationLegacy
            ? TypeDuration
            : entry.SerializationTypeId;

        WriteInt16(buffer, typeId);
        WriteInt16(buffer, (short)entry.EffectId);

        switch (typeId)
        {
            case TypeInteger:
                WriteInt16(buffer, (short)entry.Value);
                break;
            case TypeDice:
                WriteInt16(buffer, (short)entry.DiceNum);
                WriteInt16(buffer, (short)entry.DiceSide);
                WriteInt16(buffer, (short)entry.Value);
                break;
            case TypeMinMax:
                WriteInt16(buffer, (short)entry.MinValue);
                WriteInt16(buffer, (short)entry.MaxValue);
                break;
            case TypeCreature:
                WriteInt16(buffer, (short)entry.Value);
                break;
            case TypeDuration:
                WriteInt16(buffer, (short)entry.DiceNum);
                WriteInt16(buffer, (short)entry.DiceSide);
                WriteInt16(buffer, (short)entry.Value);
                break;
            case TypeBase:
                break;
            default:
                throw new InvalidOperationException($"Unsupported serialization type {typeId}.");
        }
    }

    private static short ReadInt16(byte[] data, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;
        return value;
    }

    private static short ReadInt16Safe(byte[] data, ref int offset)
    {
        if (offset + 2 > data.Length)
        {
            return 0;
        }

        return ReadInt16(data, ref offset);
    }

    private static void WriteInt16(List<byte> buffer, short value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, value);
        buffer.Add(bytes[0]);
        buffer.Add(bytes[1]);
    }
}

public sealed record SunshineEffectEntry(
    short SerializationTypeId,
    int EffectId,
    int DiceNum,
    int DiceSide,
    int Value,
    int MinValue,
    int MaxValue,
    bool IsSupported,
    string? PreservedEffectHex);

public sealed record SunshineItemEffectsDecodeResult(
    IReadOnlyList<SunshineEffectEntry> Entries,
    string? PreservedSuffixHex,
    IReadOnlyList<string> Warnings);
