// Generated / ported for Sunshine compatibility
using System;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{
    public class CharacterToRelookInformation
    {
        public const short Id = 399;
        public virtual short TypeId
        {
            get { return Id; }
        }

        public int id;
        public int cosmeticId;

        public CharacterToRelookInformation()
        {
        }

        public CharacterToRelookInformation(int id, int cosmeticId)
        {
            this.id = id;
            this.cosmeticId = cosmeticId;
        }

        public virtual void Serialize(IDataWriter writer)
        {
            writer.WriteInt(id);
            writer.WriteInt(cosmeticId);
        }

        public virtual void Deserialize(IDataReader reader)
        {
            id = reader.ReadInt();
            if (id < 0)
                throw new Exception("Forbidden value on id = " + id + ", it doesn't respect the following condition : id < 0");

            cosmeticId = reader.ReadInt();
            if (cosmeticId < 0)
                throw new Exception("Forbidden value on cosmeticId = " + cosmeticId + ", it doesn't respect the following condition : cosmeticId < 0");
        }
    }
}