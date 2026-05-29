
















// Generated on 10/13/2017 02:17:25
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class ObjectEffects
{

public const short Id = 358;
public virtual short TypeId
{
    get { return Id; }
}

public short powerRate;
        public bool overMax;
        public IEnumerable<Types.ObjectEffect> effects;
        

public ObjectEffects()
{
}

public ObjectEffects(short powerRate, bool overMax, IEnumerable<Types.ObjectEffect> effects)
        {
            this.powerRate = powerRate;
            this.overMax = overMax;
            this.effects = effects;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteShort(powerRate);
            writer.WriteBoolean(overMax);
            var effects_before = writer.Position;
            var effects_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in effects)
            {
                 writer.WriteShort(entry.TypeId);
                 entry.Serialize(writer);
                 effects_count++;
            }
            var effects_after = writer.Position;
            writer.Seek((int)effects_before);
            writer.WriteUShort((ushort)effects_count);
            writer.Seek((int)effects_after);

            

}

public virtual void Deserialize(IDataReader reader)
{

powerRate = reader.ReadShort();
            overMax = reader.ReadBoolean();
            var limit = reader.ReadUShort();
            var effects_ = new Types.ObjectEffect[limit];
            for (int i = 0; i < limit; i++)
            {
                 effects_[i] = Types.ProtocolTypeManager.GetInstance<Types.ObjectEffect>(reader.ReadShort());
                 effects_[i].Deserialize(reader);
            }
            effects = effects_;
            

}



}


}
