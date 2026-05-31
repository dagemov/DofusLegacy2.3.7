
















// Generated on 10/13/2017 02:17:21
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class FightTemporarySpellImmunityEffect : AbstractFightDispellableEffect
{

public const short Id = 366;
public override short TypeId
{
    get { return Id; }
}

public int immuneSpellId;
        

public FightTemporarySpellImmunityEffect()
{
}

public FightTemporarySpellImmunityEffect(int uid, int targetId, short turnDuration, sbyte dispelable, short spellId, int immuneSpellId)
         : base(uid, targetId, turnDuration, dispelable, spellId)
        {
            this.immuneSpellId = immuneSpellId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(immuneSpellId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            immuneSpellId = reader.ReadInt();
            

}



}


}
