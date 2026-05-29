
















// Generated on 10/13/2017 02:17:22
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

    public class MonsterBaseCharacteristic
    {

        public const short Id = 666; // Type doesn't exist
        public virtual short TypeId
        {
            get { return Id; }
        }

        public short @base;
        public short objectsAndMountBonus;
        public short alignGiftBonus;
        public short contextModif;


        public MonsterBaseCharacteristic()
        {
        }

        public MonsterBaseCharacteristic(short @base, short objectsAndMountBonus, short alignGiftBonus, short contextModif)
        {
            this.@base = @base;
            this.objectsAndMountBonus = objectsAndMountBonus;
            this.alignGiftBonus = alignGiftBonus;
            this.contextModif = contextModif;
        }


        public virtual void Serialize(IDataWriter writer)
        {

            writer.WriteShort(@base);
            writer.WriteShort(objectsAndMountBonus);
            writer.WriteShort(alignGiftBonus);
            writer.WriteShort(contextModif);


        }

        public virtual void Deserialize(IDataReader reader)
        {

            @base = reader.ReadShort();
            objectsAndMountBonus = reader.ReadShort();
            alignGiftBonus = reader.ReadShort();
            contextModif = reader.ReadShort();


        }



    }


}
