
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ServerExperienceModificatorMessage : Message
{

public const uint Id = 6237;
public override uint MessageId
{
    get { return Id; }
}

public short experiencePercent;
        

public ServerExperienceModificatorMessage()
{
}

public ServerExperienceModificatorMessage(short experiencePercent)
        {
            this.experiencePercent = experiencePercent;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(experiencePercent);
            

}

public override void Deserialize(IDataReader reader)
{

experiencePercent = reader.ReadShort();
            if (experiencePercent < 0)
                throw new Exception("Forbidden value on experiencePercent = " + experiencePercent + ", it doesn't respect the following condition : experiencePercent < 0");
            

}


}


}
