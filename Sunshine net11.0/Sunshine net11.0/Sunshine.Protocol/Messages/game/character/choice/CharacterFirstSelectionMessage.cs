
















// Generated on 10/13/2017 02:18:42
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class CharacterFirstSelectionMessage : CharacterSelectionMessage
{

public const uint Id = 6084;
public override uint MessageId
{
    get { return Id; }
}

public bool doTutorial;
        

public CharacterFirstSelectionMessage()
{
}

public CharacterFirstSelectionMessage(int id, bool doTutorial)
         : base(id)
        {
            this.doTutorial = doTutorial;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteBoolean(false);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            doTutorial = false; reader.ReadBoolean();
            

}


}


}
