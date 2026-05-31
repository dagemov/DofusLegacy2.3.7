
















// Generated on 10/13/2017 02:17:21
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class FightTemporaryBoostWeaponDamagesEffect : FightTemporaryBoostEffect
{

public const short Id = 211;
public override short TypeId
{
    get { return Id; }
}

public short weaponTypeId;
        

public FightTemporaryBoostWeaponDamagesEffect()
{
}

public FightTemporaryBoostWeaponDamagesEffect(int uid, int targetId, short turnDuration, sbyte dispelable, short spellId, short delta, short weaponTypeId)
         : base(uid, targetId, turnDuration, dispelable, spellId, delta)
        {
            this.weaponTypeId = weaponTypeId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(weaponTypeId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            weaponTypeId = reader.ReadShort();
            

}



}


}
