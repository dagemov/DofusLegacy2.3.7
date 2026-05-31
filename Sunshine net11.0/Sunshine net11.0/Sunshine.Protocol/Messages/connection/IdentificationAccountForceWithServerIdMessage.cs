
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IdentificationAccountForceWithServerIdMessage : IdentificationAccountForceMessage
{

public const uint Id = 6133;
public override uint MessageId
{
    get { return Id; }
}

public short serverId;
        

public IdentificationAccountForceWithServerIdMessage()
{
}

public IdentificationAccountForceWithServerIdMessage(Types.Version version, string login, string password, IEnumerable<Types.TrustCertificate> certificate, bool autoconnect, string forcedAccountLogin, short serverId)
         : base(version, login, password, certificate, autoconnect, forcedAccountLogin)
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
