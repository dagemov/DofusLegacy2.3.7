// Generated on 10/13/2017 02:17:26
using System;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{
    public class HouseInformationsInside
    {
        public const short Id = 218;
        public virtual short TypeId
        {
            get { return Id; }
        }

        public int houseId;
        public short modelId;
        public int ownerId;
        public string ownerName;
        public short worldX;
        public short worldY;
        public uint price;
        public bool isLocked;

        public HouseInformationsInside()
        {
        }

        public HouseInformationsInside(int houseId, short modelId, int ownerId, string ownerName, short worldX, short worldY, uint price, bool isLocked)
        {
            this.houseId = houseId;
            this.modelId = modelId;
            this.ownerId = ownerId;
            this.ownerName = ownerName;
            this.worldX = worldX;
            this.worldY = worldY;
            this.price = price;
            this.isLocked = isLocked;
        }

        public virtual void Serialize(IDataWriter writer)
        {
            writer.WriteInt(houseId);
            writer.WriteShort(modelId);
            writer.WriteInt(ownerId);
            writer.WriteUTF(ownerName);
            writer.WriteShort(worldX);
            writer.WriteShort(worldY);
            writer.WriteUInt(price);
            writer.WriteBoolean(isLocked);
        }

        public virtual void Deserialize(IDataReader reader)
        {
            houseId = reader.ReadInt();
            if (houseId < 0)
                throw new Exception("Forbidden value on houseId = " + houseId);

            modelId = reader.ReadShort();
            if (modelId < 0)
                throw new Exception("Forbidden value on modelId = " + modelId);

            ownerId = reader.ReadInt();
            ownerName = reader.ReadUTF();

            worldX = reader.ReadShort();
            if (worldX < -255 || worldX > 255)
                throw new Exception("Forbidden value on worldX = " + worldX);

            worldY = reader.ReadShort();
            if (worldY < -255 || worldY > 255)
                throw new Exception("Forbidden value on worldY = " + worldY);

            price = reader.ReadUInt();
            if (price > 4294967295)
                throw new Exception("Forbidden value on price = " + price);

            isLocked = reader.ReadBoolean();
        }
    }
}
