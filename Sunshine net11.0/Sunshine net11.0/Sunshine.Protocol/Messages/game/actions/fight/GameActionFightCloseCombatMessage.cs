
















// Generated on 10/13/2017 02:18:39
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionFightCloseCombatMessage : AbstractGameActionFightTargetedAbilityMessage
{

public const uint Id = 6116;
public override uint MessageId
{
    get { return Id; }
}

public int weaponGenericId;
        

public GameActionFightCloseCombatMessage()
{
}

public GameActionFightCloseCombatMessage(short actionId, int sourceId, short destinationCellId, sbyte critical, bool silentCast, int weaponGenericId)
         : base(actionId, sourceId, destinationCellId, critical, silentCast)
        {
            this.weaponGenericId = weaponGenericId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(weaponGenericId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            weaponGenericId = reader.ReadInt();
            if (weaponGenericId < 0)
                throw new Exception("Forbidden value on weaponGenericId = " + weaponGenericId + ", it doesn't respect the following condition : weaponGenericId < 0");
            

}


}


}
