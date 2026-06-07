using System.Buffers.Binary;
using System.Text;
using RollblackLegacy.Admin.Infrastructure.Items;

namespace RollblackLegacy.Admin.Infrastructure.Spells;

public sealed class SpellEffectsDecoder
{
    private readonly AdminProtocolCatalog _protocolCatalog;

    public SpellEffectsDecoder(AdminProtocolCatalog protocolCatalog)
    {
        _protocolCatalog = protocolCatalog;
    }

    internal SpellEffectDecodeResult DecodeSerializedHex(string? payload, string sourceLabel)
    {
        if (!HasSerializedContainer(payload))
        {
            return SpellEffectDecodeResult.Empty();
        }

        var warnings = new List<string>();
        var rows = new List<SpellEffectDecodedRow>();

        try
        {
            var bytes = Convert.FromHexString(NormalizeHex(payload));
            var reader = new BufferCursor(bytes);
            var declaredCount = reader.ReadInt16();
            if (declaredCount < 0)
            {
                throw new InvalidOperationException("El contador de effects no puede ser negativo.");
            }

            for (var index = 0; index < declaredCount; index++)
            {
                var effectId = ToSafeInt32(reader.ReadUInt32());
                var diceNum = ToSafeInt32(reader.ReadUInt32());
                var diceFace = ToSafeInt32(reader.ReadUInt32());
                var value = reader.ReadInt32();
                var delay = reader.ReadInt32();
                var duration = reader.ReadInt32();
                var targetType = reader.ReadInt32();
                reader.SkipUtf();
                var zoneMinSize = ToSafeInt32(reader.ReadUInt32());
                var zoneSize = ToSafeInt32(reader.ReadUInt32());
                var zoneShape = ToSafeInt32(reader.ReadUInt32());
                reader.ReadBoolean();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadBoolean();

                var operatorMode = ResolveSerializedOperatorMode(effectId, diceNum, diceFace);
                rows.Add(BuildRow(
                    index,
                    effectId,
                    operatorMode,
                    value,
                    minValue: operatorMode == "Dice" ? diceNum : 0,
                    maxValue: operatorMode == "Dice" ? diceFace : 0,
                    delay: delay,
                    random: null,
                    duration,
                    targetType,
                    zoneShape,
                    zoneMinSize,
                    zoneSize));
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"No se pudo decodificar {sourceLabel}: {ex.Message}");
        }

        return new SpellEffectDecodeResult(sourceLabel, rows, warnings);
    }

    internal SpellEffectDecodeResult DecodeLegacyBinary(byte[]? payload, string sourceLabel)
    {
        if (payload is null || payload.Length == 0)
        {
            return SpellEffectDecodeResult.Empty();
        }

        var warnings = new List<string>();
        var rows = new List<SpellEffectDecodedRow>();

        try
        {
            var reader = new BufferCursor(payload);
            while (reader.BytesAvailable > 0)
            {
                var index = rows.Count;
                var serializationId = reader.ReadByte();
                var effectId = reader.ReadInt32();
                var random = ToSafeInt32(reader.ReadUInt32());
                var duration = reader.ReadInt16();
                var targetType = reader.ReadUInt16();
                var zoneShape = reader.ReadByte();
                var zoneSize = reader.ReadByte();

                switch (serializationId)
                {
                    case 1:
                        rows.Add(BuildRow(
                            index,
                            effectId,
                            "Base",
                            value: 0,
                            minValue: 0,
                            maxValue: 0,
                            delay: null,
                            random: random,
                            duration,
                            targetType,
                            zoneShape,
                            zoneMinSize: 0,
                            zoneSize: zoneSize));
                        break;

                    case 4:
                        rows.Add(BuildRow(
                            index,
                            effectId,
                            "Dice",
                            reader.ReadInt16(),
                            reader.ReadInt16(),
                            reader.ReadInt16(),
                            delay: null,
                            random: random,
                            duration,
                            targetType,
                            zoneShape,
                            zoneMinSize: 0,
                            zoneSize: zoneSize));
                        break;

                    case 6:
                        rows.Add(BuildRow(
                            index,
                            effectId,
                            "Integer",
                            reader.ReadInt16(),
                            minValue: 0,
                            maxValue: 0,
                            delay: null,
                            random: random,
                            duration,
                            targetType,
                            zoneShape,
                            zoneMinSize: 0,
                            zoneSize: zoneSize));
                        break;

                    default:
                        warnings.Add(
                            $"Se encontro un serializationId legacy no soportado ({serializationId}) en {sourceLabel}. Se devolvieron solo las filas parseadas antes del corte.");
                        return new SpellEffectDecodeResult(sourceLabel, rows, warnings);
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"No se pudo decodificar {sourceLabel}: {ex.Message}");
        }

        return new SpellEffectDecodeResult(sourceLabel, rows, warnings);
    }

    internal static bool HasSerializedContainer(string? payload)
    {
        var normalized = (payload ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(normalized) &&
               !string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase);
    }

    private SpellEffectDecodedRow BuildRow(
        int rowIndex,
        int effectId,
        string operatorMode,
        int value,
        int minValue,
        int maxValue,
        int? delay,
        int? random,
        int duration,
        int targetType,
        int zoneShape,
        int zoneMinSize,
        int zoneSize)
    {
        var protocolName = _protocolCatalog.GetEffectName(effectId);
        var label = ItemEffectDisplayMetadata.GetDisplayLabel(effectId, protocolName);
        var group = ItemEffectDisplayMetadata.ResolveGroup(effectId, protocolName, label);

        return new SpellEffectDecodedRow(
            rowIndex,
            effectId,
            label,
            protocolName,
            group,
            operatorMode,
            value,
            minValue,
            maxValue,
            delay,
            random,
            duration,
            targetType,
            zoneShape,
            zoneMinSize,
            zoneSize,
            BuildPreviewText(label, operatorMode, value, minValue, maxValue));
    }

    private static string ResolveSerializedOperatorMode(int effectId, int diceNum, int diceFace)
    {
        if (diceNum != 0 || diceFace != 0)
        {
            return "Dice";
        }

        var protocolName = $"Effect_{effectId}";
        return ItemEffectDisplayMetadata.SuggestFormat(effectId, protocolName) == "Dice"
            ? "Dice"
            : "Integer";
    }

    private static string BuildPreviewText(
        string label,
        string operatorMode,
        int value,
        int minValue,
        int maxValue)
    {
        return operatorMode switch
        {
            "Dice" => $"{label}: valor={value}, min={minValue}, max={maxValue}",
            "Base" => label,
            _ => $"{label}: valor={value}",
        };
    }

    private static int ToSafeInt32(uint value) =>
        value > int.MaxValue ? int.MaxValue : (int)value;

    private static string NormalizeHex(string? payload)
    {
        var normalized = (payload ?? string.Empty).Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (normalized.IndexOfAny([' ', '\t', '\r', '\n']) < 0)
        {
            return normalized;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    internal sealed record SpellEffectDecodeResult(
        string? Source,
        IReadOnlyList<SpellEffectDecodedRow> Rows,
        IReadOnlyList<string> Warnings)
    {
        public static SpellEffectDecodeResult Empty() =>
            new(null, Array.Empty<SpellEffectDecodedRow>(), Array.Empty<string>());
    }

    internal sealed record SpellEffectDecodedRow(
        int RowIndex,
        int EffectId,
        string Label,
        string ProtocolName,
        string Group,
        string OperatorMode,
        int Value,
        int MinValue,
        int MaxValue,
        int? Delay,
        int? Random,
        int Duration,
        int TargetType,
        int ZoneShape,
        int ZoneMinSize,
        int ZoneSize,
        string PreviewText);

    private sealed class BufferCursor
    {
        private readonly byte[] _buffer;
        private int _offset;

        public BufferCursor(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int BytesAvailable => _buffer.Length - _offset;

        public byte ReadByte()
        {
            EnsureAvailable(1);
            return _buffer[_offset++];
        }

        public bool ReadBoolean() => ReadByte() == 1;

        public short ReadInt16()
        {
            EnsureAvailable(2);
            var value = BinaryPrimitives.ReadInt16BigEndian(_buffer.AsSpan(_offset, 2));
            _offset += 2;
            return value;
        }

        public ushort ReadUInt16()
        {
            EnsureAvailable(2);
            var value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(_offset, 2));
            _offset += 2;
            return value;
        }

        public int ReadInt32()
        {
            EnsureAvailable(4);
            var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(_offset, 4));
            _offset += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureAvailable(4);
            var value = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(_offset, 4));
            _offset += 4;
            return value;
        }

        public void SkipUtf()
        {
            var length = ReadUInt16();
            Skip(length);
        }

        public void Skip(int count)
        {
            EnsureAvailable(count);
            _offset += count;
        }

        private void EnsureAvailable(int count)
        {
            if (count < 0 || BytesAvailable < count)
            {
                throw new InvalidOperationException("El payload de effects termino antes de tiempo.");
            }
        }
    }
}
