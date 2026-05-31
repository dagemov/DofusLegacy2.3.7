
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IdentificationMessage : Message
{

public const uint Id = 4;
public override uint MessageId
{
    get { return Id; }
}

public Types.Version version;
        public string login;
        public string password;
        public IEnumerable<Types.TrustCertificate> certificate;
        public bool autoconnect;
        

public IdentificationMessage()
{
}

public IdentificationMessage(Types.Version version, string login, string password, IEnumerable<Types.TrustCertificate> certificate, bool autoconnect)
        {
            this.version = version;
            this.login = login;
            this.password = password;
            this.certificate = certificate;
            this.autoconnect = autoconnect;
        }
        

public override void Serialize(IDataWriter writer)
{

version.Serialize(writer);
            writer.WriteUTF(login);
            writer.WriteUTF(password);
            var certificate_before = writer.Position;
            var certificate_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in certificate)
            {
                 entry.Serialize(writer);
                 certificate_count++;
            }
            var certificate_after = writer.Position;
            writer.Seek((int)certificate_before);
            writer.WriteUShort((ushort)certificate_count);
            writer.Seek((int)certificate_after);

            writer.WriteBoolean(autoconnect);
            

}

public override void Deserialize(IDataReader reader)
{

version = new Types.Version();
            version.Deserialize(reader);
            login = reader.ReadUTF();
            password = reader.ReadUTF();
            var limit = reader.ReadUShort();
            var certificate_ = new Types.TrustCertificate[limit];
            for (int i = 0; i < limit; i++)
            {
                 certificate_[i] = new Types.TrustCertificate();
                 certificate_[i].Deserialize(reader);
            }
            certificate = certificate_;
            autoconnect = reader.ReadBoolean();
            

}


}


}
