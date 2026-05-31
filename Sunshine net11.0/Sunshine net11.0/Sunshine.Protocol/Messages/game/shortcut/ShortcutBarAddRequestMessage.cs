
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ShortcutBarAddRequestMessage : Message
{

public const uint Id = 6225;
public override uint MessageId
{
    get { return Id; }
}

public byte barType;
        public Types.Shortcut shortcut;
        

public ShortcutBarAddRequestMessage()
{
}

public ShortcutBarAddRequestMessage(byte barType, Types.Shortcut shortcut)
        {
            this.barType = barType;
            this.shortcut = shortcut;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteByte(barType);
            writer.WriteShort(shortcut.TypeId);
            shortcut.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

barType = reader.ReadByte();
            barType--;
            if (barType < 0)
                throw new Exception("Forbidden value on barType = " + barType + ", it doesn't respect the following condition : barType < 0");
            shortcut = Types.ProtocolTypeManager.GetInstance<Types.Shortcut>(reader.ReadShort());
            shortcut.Deserialize(reader);
            

}


}


}
