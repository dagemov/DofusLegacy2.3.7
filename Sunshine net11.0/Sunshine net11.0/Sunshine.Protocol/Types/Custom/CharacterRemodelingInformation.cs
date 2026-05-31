using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{
    public class CharacterRemodelingInformation
    {
        public const short Id = 479;
        public virtual short TypeId => Id;

        public int id;
        public string name;
        public sbyte breed;
        public bool sex;
        public short cosmeticId;
        public IEnumerable<int> colors;

        public CharacterRemodelingInformation()
        {
            name = string.Empty;
            colors = Array.Empty<int>();
        }

        public CharacterRemodelingInformation(int id, string name, sbyte breed, bool sex, short cosmeticId, IEnumerable<int> colors)
        {
            this.id = id;
            this.name = name ?? string.Empty;
            this.breed = breed;
            this.sex = sex;
            this.cosmeticId = cosmeticId;
            this.colors = colors ?? Array.Empty<int>();
        }

        public virtual void Serialize(IDataWriter writer)
        {
            writer.WriteInt(id);
            writer.WriteUTF(name ?? string.Empty);
            writer.WriteSByte(breed);
            writer.WriteBoolean(sex);
            writer.WriteShort(cosmeticId);

            var entries = (colors ?? Array.Empty<int>()).ToArray();
            writer.WriteUShort((ushort)entries.Length);
            foreach (var entry in entries)
                writer.WriteInt(entry);
        }

        public virtual void Deserialize(IDataReader reader)
        {
            id = reader.ReadInt();
            name = reader.ReadUTF();
            breed = reader.ReadSByte();
            sex = reader.ReadBoolean();
            cosmeticId = reader.ReadShort();

            var limit = reader.ReadUShort();
            var values = new int[limit];
            for (int i = 0; i < limit; i++)
                values[i] = reader.ReadInt();

            colors = values;
        }
    }
}
