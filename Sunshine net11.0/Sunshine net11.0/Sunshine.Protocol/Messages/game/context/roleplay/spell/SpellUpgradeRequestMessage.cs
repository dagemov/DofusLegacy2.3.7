
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class SpellUpgradeRequestMessage : Message
{

public const uint Id = 5608;
public override uint MessageId
{
    get { return Id; }
}

public short spellId;
        

public SpellUpgradeRequestMessage()
{
}

public SpellUpgradeRequestMessage(short spellId)
        {
            this.spellId = spellId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(spellId);
            

}

public override void Deserialize(IDataReader reader)
{

spellId = reader.ReadShort();
            if (spellId < 0)
                throw new Exception("Forbidden value on spellId = " + spellId + ", it doesn't respect the following condition : spellId < 0");
            

}


}


}
