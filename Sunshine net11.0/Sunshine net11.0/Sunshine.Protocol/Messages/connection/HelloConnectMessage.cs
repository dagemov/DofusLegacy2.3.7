
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HelloConnectMessage : Message
{

public const uint Id = 3;
public override uint MessageId
{
    get { return Id; }
}

public sbyte connectionType;
        public string key;
        

public HelloConnectMessage()
{
}

public HelloConnectMessage(sbyte connectionType, string key)
        {
            this.connectionType = connectionType;
            this.key = key;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(connectionType);
            writer.WriteUTF(key);
            

}

public override void Deserialize(IDataReader reader)
{

connectionType = reader.ReadSByte();
            if (connectionType < 0)
                throw new Exception("Forbidden value on connectionType = " + connectionType + ", it doesn't respect the following condition : connectionType < 0");
            key = reader.ReadUTF();
            

}


}


}
