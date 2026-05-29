using Dapper.Contrib.Extensions;
using Sunshine.Protocol.Types;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sunshine.MySql.Database.World.Characters.Items
{
    [Table("characters_items_presets")]
    public class CharacterPresetRecord
    {
        [Key]
        public int Id { get; set; }
        public int PresetId { get; set; }
        public int OwnerId { get; set; }
        public int SymbolId { get; set; }
        public byte[] SerializedObjects { get; set; }

        [Write(false)]
        public List<PresetItem> Objects { get; set; } = new List<PresetItem>();

        public void EnsureDeserialized()
        {
            if (Objects != null && Objects.Count > 0)
                return;

            Objects = Deserialize(SerializedObjects);
        }

        public Preset GetNetworkPreset()
        {
            EnsureDeserialized();
            var objects = Objects ?? new List<PresetItem>();
            var hasMount = objects.Any(x => x != null && (x.position == (byte)Protocol.Enums.CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT || x.position == (byte)Protocol.Enums.CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS));
            return new Preset((sbyte)PresetId, (sbyte)SymbolId, hasMount, objects);
        }

        public void SetObjects(IEnumerable<PresetItem> objects)
        {
            Objects = objects?.ToList() ?? new List<PresetItem>();
            SerializedObjects = Serialize(Objects);
        }

        public static byte[] Serialize(IEnumerable<PresetItem> objects)
        {
            var buffer = new List<byte>();

            foreach (var preset in objects ?? Enumerable.Empty<PresetItem>())
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(preset.objUid);
                    writer.Write(preset.objGid);
                    writer.Write(preset.position);
                    writer.Flush();
                    buffer.AddRange(ms.ToArray());
                }
            }

            return buffer.ToArray();
        }

        public static List<PresetItem> Deserialize(byte[] buffer)
        {
            var presetObjects = new List<PresetItem>();

            if (buffer == null || buffer.Length == 0)
                return presetObjects;

            using (var ms = new MemoryStream(buffer))
            using (var reader = new BinaryReader(ms))
            {
                while (reader.BaseStream.Position < buffer.Length)
                {
                    var objUid = reader.ReadInt32();
                    var objGid = reader.ReadInt32();
                    var position = reader.ReadByte();

                    presetObjects.Add(new PresetItem(position, objGid, objUid));
                }
            }

            return presetObjects;
        }
    }
}
