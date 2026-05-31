
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class JobCrafterDirectoryDefineSettingsMessage : Message
{

public const uint Id = 5649;
public override uint MessageId
{
    get { return Id; }
}

public Types.JobCrafterDirectorySettings settings;
        

public JobCrafterDirectoryDefineSettingsMessage()
{
}

public JobCrafterDirectoryDefineSettingsMessage(Types.JobCrafterDirectorySettings settings)
        {
            this.settings = settings;
        }
        

public override void Serialize(IDataWriter writer)
{

settings.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

settings = new Types.JobCrafterDirectorySettings();
            settings.Deserialize(reader);
            

}


}


}
