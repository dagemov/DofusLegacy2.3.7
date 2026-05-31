
















// Generated on 10/13/2017 02:18:45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ShowCellSpectatorMessage : ShowCellMessage
{

public const uint Id = 6158;
public override uint MessageId
{
    get { return Id; }
}

public string playerName;
        

public ShowCellSpectatorMessage()
{
}

public ShowCellSpectatorMessage(int sourceId, short cellId, string playerName)
         : base(sourceId, cellId)
        {
            this.playerName = playerName;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteUTF(playerName);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            playerName = reader.ReadUTF();
            

}


}


}
