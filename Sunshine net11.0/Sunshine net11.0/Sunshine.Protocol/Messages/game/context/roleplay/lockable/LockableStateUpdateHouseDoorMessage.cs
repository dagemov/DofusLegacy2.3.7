
















// Generated on 10/13/2017 02:18:51
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class LockableStateUpdateHouseDoorMessage : LockableStateUpdateAbstractMessage
{

public const uint Id = 5668;
public override uint MessageId
{
    get { return Id; }
}

public int houseId;
        

public LockableStateUpdateHouseDoorMessage()
{
}

public LockableStateUpdateHouseDoorMessage(bool locked, int houseId)
         : base(locked)
        {
            this.houseId = houseId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(houseId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            houseId = reader.ReadInt();
            

}


}


}
