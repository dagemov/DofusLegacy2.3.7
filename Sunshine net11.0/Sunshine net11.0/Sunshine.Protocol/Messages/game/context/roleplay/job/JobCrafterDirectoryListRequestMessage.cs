
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class JobCrafterDirectoryListRequestMessage : Message
{

public const uint Id = 6047;
public override uint MessageId
{
    get { return Id; }
}

public sbyte jobId;
        

public JobCrafterDirectoryListRequestMessage()
{
}

public JobCrafterDirectoryListRequestMessage(sbyte jobId)
        {
            this.jobId = jobId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(jobId);
            

}

public override void Deserialize(IDataReader reader)
{

jobId = reader.ReadSByte();
            if (jobId < 0)
                throw new Exception("Forbidden value on jobId = " + jobId + ", it doesn't respect the following condition : jobId < 0");
            

}


}


}
