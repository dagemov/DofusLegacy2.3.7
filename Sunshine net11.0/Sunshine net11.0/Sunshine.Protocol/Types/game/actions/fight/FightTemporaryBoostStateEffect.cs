
















// Generated on 10/13/2017 02:17:21
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class FightTemporaryBoostStateEffect : FightTemporaryBoostEffect
{

public const short Id = 214;
public override short TypeId
{
    get { return Id; }
}

public short stateId;
        

public FightTemporaryBoostStateEffect()
{
}

public FightTemporaryBoostStateEffect(int uid, int targetId, short turnDuration, sbyte dispelable, short spellId, short delta, short stateId)
         : base(uid, targetId, turnDuration, dispelable, spellId, delta)
        {
            this.stateId = stateId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(stateId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            stateId = reader.ReadShort();
            

}



}


}
