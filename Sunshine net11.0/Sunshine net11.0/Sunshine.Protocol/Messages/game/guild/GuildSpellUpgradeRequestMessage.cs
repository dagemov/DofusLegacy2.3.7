
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildSpellUpgradeRequestMessage : Message
{

public const uint Id = 5699;
public override uint MessageId
{
    get { return Id; }
}

public int spellId;
        

public GuildSpellUpgradeRequestMessage()
{
}

public GuildSpellUpgradeRequestMessage(int spellId)
        {
            this.spellId = spellId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(spellId);
            

}

public override void Deserialize(IDataReader reader)
{

spellId = reader.ReadInt();
            if (spellId < 0)
                throw new Exception("Forbidden value on spellId = " + spellId + ", it doesn't respect the following condition : spellId < 0");
            

}


}


}
