using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types.Custom
{
    public class RemodelingInformation
    {
        public const short Id = 480;
        public virtual short TypeId => Id;

        public string Name { get; set; }
        public sbyte Breed { get; set; }
        public bool Sex { get; set; }
        public short CosmeticId { get; set; }
        public IEnumerable<int> Colors { get; set; }

        public RemodelingInformation()
        {
            Colors = Array.Empty<int>();
            Name = string.Empty;
        }

        public RemodelingInformation(string name, sbyte breed, bool sex, short cosmeticId, IEnumerable<int> colors)
        {
            Name = name ?? string.Empty;
            Breed = breed;
            Sex = sex;
            CosmeticId = cosmeticId;
            Colors = colors ?? Array.Empty<int>();
        }

        public virtual void Serialize(IDataWriter writer)
        {
            writer.WriteUTF(Name ?? string.Empty);
            writer.WriteSByte(Breed);
            writer.WriteBoolean(Sex);
            writer.WriteShort(CosmeticId);

            var entries = (Colors ?? Array.Empty<int>()).ToArray();
            writer.WriteUShort((ushort)entries.Length);
            foreach (var entry in entries)
                writer.WriteInt(entry);
        }

        public virtual void Deserialize(IDataReader reader)
        {
            Name = reader.ReadUTF();
            Breed = reader.ReadSByte();
            Sex = reader.ReadBoolean();
            CosmeticId = reader.ReadShort();

            var limit = reader.ReadUShort();
            var colors = new int[limit];
            for (int i = 0; i < limit; i++)
                colors[i] = reader.ReadInt();

            Colors = colors;
        }
    }
}
