using System.IO.Compression;
using System.Text;

namespace Rollback.Admin.Services;

internal static class ClientSwfAbcSupport
{
    public static byte[] ReadSwfBody(byte[] swfBytes)
    {
        var signature = Encoding.ASCII.GetString(swfBytes, 0, 3);
        if (signature == "FWS")
            return swfBytes[8..];

        if (signature == "CWS")
        {
            using var input = new MemoryStream(swfBytes, 8, swfBytes.Length - 8);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }

        throw new InvalidDataException("Unsupported SWF signature.");
    }

    public static IEnumerable<byte[]> EnumerateDoAbcPayloads(byte[] swfBody)
    {
        var offset = GetTagStartOffset(swfBody);
        while (offset + 2 <= swfBody.Length)
        {
            var header = BitConverter.ToUInt16(swfBody, offset);
            var tagCode = header >> 6;
            var tagLength = header & 0x3F;
            offset += 2;

            if (tagLength == 0x3F)
            {
                if (offset + 4 > swfBody.Length)
                    yield break;

                tagLength = BitConverter.ToInt32(swfBody, offset);
                offset += 4;
            }

            if (offset + tagLength > swfBody.Length)
                yield break;

            if (tagCode == 82)
                yield return swfBody[offset..(offset + tagLength)];

            offset += tagLength;
            if (tagCode == 0)
                yield break;
        }
    }

    public static bool TryReadLiteral(byte[] code, ref int offset, AbcFile abc, out object? value)
    {
        value = null;
        if (!TryReadInstructionByte(code, ref offset, out var opcode))
            return false;

        switch (opcode)
        {
            case 0x20:
            case 0x21:
                value = null;
                return true;

            case 0x24:
                if (offset >= code.Length)
                    return false;

                value = unchecked((sbyte)code[offset++]);
                return true;

            case 0x25:
                if (!TryReadU30(code, ref offset, out var pushShortValue))
                    return false;

                value = pushShortValue;
                return true;

            case 0x26:
                value = true;
                return true;

            case 0x27:
                value = false;
                return true;

            case 0x2C:
                if (!TryReadU30(code, ref offset, out var stringIndex))
                    return false;

                value = abc.ResolveString(stringIndex);
                return true;

            case 0x2D:
                if (!TryReadU30(code, ref offset, out var intIndex))
                    return false;

                value = abc.ResolveInt(intIndex);
                return true;

            case 0x2E:
                if (!TryReadU30(code, ref offset, out var uintIndex))
                    return false;

                value = abc.ResolveUInt(uintIndex);
                return true;

            case 0x2F:
                if (!TryReadU30(code, ref offset, out var doubleIndex))
                    return false;

                value = abc.ResolveDouble(doubleIndex);
                return true;

            default:
                return false;
        }
    }

    public static bool TryReadInstructionByte(byte[] code, ref int offset, out byte value)
    {
        value = 0;
        if (offset >= code.Length)
            return false;

        value = code[offset++];
        return true;
    }

    public static bool TryReadU30(byte[] code, ref int offset, out int value)
    {
        value = 0;
        var shift = 0;
        byte current;

        do
        {
            if (offset >= code.Length)
                return false;

            current = code[offset++];
            value |= (current & 0x7F) << shift;
            shift += 7;
        }
        while ((current & 0x80) != 0 && shift < 35);

        return true;
    }

    public static bool TryToInt(object? value, out int result)
    {
        result = 0;
        switch (value)
        {
            case int intValue:
                result = intValue;
                return true;
            case uint uintValue when uintValue <= int.MaxValue:
                result = (int)uintValue;
                return true;
            case sbyte sbyteValue:
                result = sbyteValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case ushort ushortValue:
                result = ushortValue;
                return true;
            case double doubleValue when doubleValue >= int.MinValue && doubleValue <= int.MaxValue:
                result = (int)doubleValue;
                return true;
            default:
                return false;
        }
    }

    public static bool TryToShort(object? value, out short result)
    {
        result = 0;
        if (!TryToInt(value, out var intValue) || intValue < short.MinValue || intValue > short.MaxValue)
            return false;

        result = (short)intValue;
        return true;
    }

    public static byte[] BuildCallPropertyPattern(int multinameIndex, int argumentCount)
    {
        var bytes = new List<byte> { 0x46 };
        bytes.AddRange(EncodeU30(multinameIndex));
        bytes.AddRange(EncodeU30(argumentCount));
        return bytes.ToArray();
    }

    public static IEnumerable<byte> EncodeU30(int value)
    {
        do
        {
            var next = (byte)(value & 0x7F);
            value >>= 7;
            if (value > 0)
                next |= 0x80;

            yield return next;
        }
        while (value > 0);
    }

    public static int IndexOf(byte[] haystack, byte[] needle, int startIndex, int endExclusive)
    {
        var maxStart = Math.Min(endExclusive - needle.Length, haystack.Length - needle.Length);
        for (var i = startIndex; i <= maxStart; i++)
        {
            var matched = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] == needle[j])
                    continue;

                matched = false;
                break;
            }

            if (matched)
                return i;
        }

        return -1;
    }

    public static string? FindCommonDirectory(string requiredSwfFile)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "client", "app", "data", "common");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, requiredSwfFile)))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    public static string ReadZeroTerminatedString(BinaryReader reader)
    {
        using var ms = new MemoryStream();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var value = reader.ReadByte();
            if (value == 0)
                break;

            ms.WriteByte(value);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static int GetTagStartOffset(byte[] body)
    {
        var bitOffset = 0;
        var nbits = ReadBits(body, ref bitOffset, 5);
        _ = ReadBits(body, ref bitOffset, nbits);
        _ = ReadBits(body, ref bitOffset, nbits);
        _ = ReadBits(body, ref bitOffset, nbits);
        _ = ReadBits(body, ref bitOffset, nbits);

        var byteOffset = (bitOffset + 7) / 8;
        byteOffset += 2;
        byteOffset += 2;
        return byteOffset;
    }

    private static int ReadBits(byte[] data, ref int bitOffset, int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            var absoluteBit = bitOffset + i;
            var currentByte = data[absoluteBit / 8];
            var bitIndex = 7 - (absoluteBit % 8);
            value = (value << 1) | ((currentByte >> bitIndex) & 1);
        }

        bitOffset += count;
        return value;
    }

    internal sealed class AbcFile
    {
        private byte[] _abcBytes;
        private int _offset;

        public List<int> IntPool { get; } = new() { 0 };
        public List<uint> UIntPool { get; } = new() { 0 };
        public List<double> DoublePool { get; } = new() { 0 };
        public List<string> StringPool { get; } = new() { string.Empty };
        public List<string> NamespacePool { get; } = new() { string.Empty };
        public List<MultinameInfo> MultinamePool { get; } = new() { new(string.Empty) };
        public List<byte[]> MethodBodies { get; } = new();

        public AbcFile(byte[] abcBytes) =>
            _abcBytes = abcBytes;

        public void Parse()
        {
            using var reader = new BinaryReader(new MemoryStream(_abcBytes, writable: false));
            _ = reader.ReadUInt32();
            ClientSwfAbcSupport.ReadZeroTerminatedString(reader);
            _abcBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            _offset = 0;

            _ = ReadU16();
            _ = ReadU16();
            ReadConstantPool();
            ReadRemainingSections();
        }

        public int FindMultinameIndex(string name)
        {
            for (var i = 1; i < MultinamePool.Count; i++)
            {
                if (string.Equals(MultinamePool[i].Name, name, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        public IEnumerable<int> FindMultinameIndices(string name)
        {
            for (var i = 1; i < MultinamePool.Count; i++)
            {
                if (string.Equals(MultinamePool[i].Name, name, StringComparison.Ordinal))
                    yield return i;
            }
        }

        public int ResolveInt(int index) =>
            index >= 0 && index < IntPool.Count ? IntPool[index] : 0;

        public uint ResolveUInt(int index) =>
            index >= 0 && index < UIntPool.Count ? UIntPool[index] : 0;

        public double ResolveDouble(int index) =>
            index >= 0 && index < DoublePool.Count ? DoublePool[index] : 0;

        public string ResolveString(int index) =>
            index >= 0 && index < StringPool.Count ? StringPool[index] : string.Empty;

        private void ReadConstantPool()
        {
            var intCount = ReadU30();
            for (var i = 1; i < intCount; i++)
                IntPool.Add(ReadS32());

            var uintCount = ReadU30();
            for (var i = 1; i < uintCount; i++)
                UIntPool.Add((uint)ReadU30());

            var doubleCount = ReadU30();
            for (var i = 1; i < doubleCount; i++)
                DoublePool.Add(BitConverter.ToDouble(ReadBytes(8), 0));

            var stringCount = ReadU30();
            for (var i = 1; i < stringCount; i++)
            {
                var size = ReadU30();
                StringPool.Add(Encoding.UTF8.GetString(ReadBytes(size)));
            }

            var namespaceCount = ReadU30();
            for (var i = 1; i < namespaceCount; i++)
            {
                _ = ReadByte();
                var nameIndex = ReadU30();
                NamespacePool.Add(nameIndex > 0 && nameIndex < StringPool.Count ? StringPool[nameIndex] : string.Empty);
            }

            var namespaceSetCount = ReadU30();
            for (var i = 1; i < namespaceSetCount; i++)
            {
                var count = ReadU30();
                for (var j = 0; j < count; j++)
                    _ = ReadU30();
            }

            var multinameCount = ReadU30();
            for (var i = 1; i < multinameCount; i++)
                MultinamePool.Add(ReadMultiname());
        }

        private void ReadRemainingSections()
        {
            var methodCount = ReadU30();
            for (var i = 0; i < methodCount; i++)
            {
                var paramCount = ReadU30();
                _ = ReadU30();
                for (var j = 0; j < paramCount; j++)
                    _ = ReadU30();

                _ = ReadU30();
                var flags = ReadByte();

                if ((flags & 0x08) != 0)
                {
                    var optionCount = ReadU30();
                    for (var j = 0; j < optionCount; j++)
                    {
                        _ = ReadU30();
                        _ = ReadByte();
                    }
                }

                if ((flags & 0x80) != 0)
                {
                    for (var j = 0; j < paramCount; j++)
                        _ = ReadU30();
                }
            }

            var metadataCount = ReadU30();
            for (var i = 0; i < metadataCount; i++)
            {
                _ = ReadU30();
                var itemCount = ReadU30();
                for (var j = 0; j < itemCount; j++)
                    _ = ReadU30();
                for (var j = 0; j < itemCount; j++)
                    _ = ReadU30();
            }

            var classCount = ReadU30();
            for (var i = 0; i < classCount; i++)
            {
                _ = ReadU30();
                _ = ReadU30();
                var flags = ReadByte();
                if ((flags & 0x08) != 0)
                    _ = ReadU30();

                var interfaceCount = ReadU30();
                for (var j = 0; j < interfaceCount; j++)
                    _ = ReadU30();

                _ = ReadU30();
                SkipTraits();
            }

            for (var i = 0; i < classCount; i++)
            {
                _ = ReadU30();
                SkipTraits();
            }

            var scriptCount = ReadU30();
            for (var i = 0; i < scriptCount; i++)
            {
                _ = ReadU30();
                SkipTraits();
            }

            var methodBodyCount = ReadU30();
            for (var i = 0; i < methodBodyCount; i++)
            {
                _ = ReadU30();
                _ = ReadU30();
                _ = ReadU30();
                _ = ReadU30();
                _ = ReadU30();
                var codeLength = ReadU30();
                MethodBodies.Add(ReadBytes(codeLength));

                var exceptionCount = ReadU30();
                for (var j = 0; j < exceptionCount; j++)
                {
                    _ = ReadU30();
                    _ = ReadU30();
                    _ = ReadU30();
                    _ = ReadU30();
                    _ = ReadU30();
                }

                SkipTraits();
            }
        }

        private void SkipTraits()
        {
            var traitCount = ReadU30();
            for (var i = 0; i < traitCount; i++)
            {
                _ = ReadU30();
                var kind = ReadByte();
                var tag = kind & 0x0F;
                var attrs = (kind >> 4) & 0x0F;

                switch (tag)
                {
                    case 0:
                    case 6:
                        _ = ReadU30();
                        _ = ReadU30();
                        var valueIndex = ReadU30();
                        if (valueIndex != 0)
                            _ = ReadByte();
                        break;

                    case 1:
                    case 2:
                    case 3:
                        _ = ReadU30();
                        _ = ReadU30();
                        break;

                    case 4:
                    case 5:
                        _ = ReadU30();
                        _ = ReadU30();
                        break;
                }

                if ((attrs & 0x04) != 0)
                {
                    var metadataCount = ReadU30();
                    for (var j = 0; j < metadataCount; j++)
                        _ = ReadU30();
                }
            }
        }

        private MultinameInfo ReadMultiname()
        {
            var kind = ReadByte();
            return kind switch
            {
                0x07 or 0x0D => ReadQName(kind == 0x0D),
                0x0F or 0x10 => new MultinameInfo(ResolveString(ReadU30()), string.Empty, kind == 0x10),
                0x11 or 0x12 => new MultinameInfo(string.Empty),
                0x09 or 0x0E => new MultinameInfo(ResolveString(ReadU30()), string.Empty, kind == 0x0E, false, false, ReadU30()),
                0x1B or 0x1C => new MultinameInfo(string.Empty, string.Empty, false, true, false, ReadU30()),
                0x1D => ReadTypeName(),
                _ => new MultinameInfo(string.Empty),
            };
        }

        private MultinameInfo ReadQName(bool isAttribute)
        {
            var namespaceIndex = ReadU30();
            var nameIndex = ReadU30();
            return new MultinameInfo(ResolveString(nameIndex), ResolveNamespace(namespaceIndex), isAttribute);
        }

        private MultinameInfo ReadTypeName()
        {
            var qnameIndex = ReadU30();
            var paramCount = ReadU30();
            for (var i = 0; i < paramCount; i++)
                _ = ReadU30();

            var name = qnameIndex > 0 && qnameIndex < MultinamePool.Count
                ? MultinamePool[qnameIndex].Name
                : string.Empty;

            return new MultinameInfo(name, string.Empty, false, false, false, 0, qnameIndex);
        }

        private string ResolveNamespace(int index) =>
            index > 0 && index < NamespacePool.Count ? NamespacePool[index] : string.Empty;

        private byte ReadByte() => _abcBytes[_offset++];

        private ushort ReadU16()
        {
            var value = BitConverter.ToUInt16(_abcBytes, _offset);
            _offset += 2;
            return value;
        }

        private byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            Array.Copy(_abcBytes, _offset, bytes, 0, count);
            _offset += count;
            return bytes;
        }

        private int ReadS32()
        {
            var result = 0;
            var shift = 0;
            byte current;
            do
            {
                current = _abcBytes[_offset++];
                result |= (current & 0x7F) << shift;
                shift += 7;
            }
            while ((current & 0x80) != 0 && shift < 35);

            if ((shift < 32) && (current & 0x40) != 0)
                result |= -1 << shift;

            return result;
        }

        private int ReadU30()
        {
            var result = 0;
            var shift = 0;
            byte current;
            do
            {
                current = _abcBytes[_offset++];
                result |= (current & 0x7F) << shift;
                shift += 7;
            }
            while ((current & 0x80) != 0 && shift < 35);

            return result;
        }
    }

    internal readonly record struct MultinameInfo(
        string Name,
        string Namespace = "",
        bool IsAttribute = false,
        bool IsRuntimeName = false,
        bool IsRuntimeNamespace = false,
        int NamespaceSetIndex = 0,
        int TypeNameIndex = 0);
}
