
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IdentificationWithLoginTokenAndServerIdMessage : IdentificationWithLoginTokenMessage
{

public const uint Id = 6200;
public override uint MessageId
{
    get { return Id; }
}

public short serverId;
        

public IdentificationWithLoginTokenAndServerIdMessage()
{
}

public IdentificationWithLoginTokenAndServerIdMessage(Types.Version version, string login, string password, IEnumerable<Types.TrustCertificate> certificate, bool autoconnect, string loginToken, short serverId)
         : base(version, login, password, certificate, autoconnect, loginToken)
        {
            this.serverId = serverId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(serverId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            serverId = reader.ReadShort();
            

}


}


}
