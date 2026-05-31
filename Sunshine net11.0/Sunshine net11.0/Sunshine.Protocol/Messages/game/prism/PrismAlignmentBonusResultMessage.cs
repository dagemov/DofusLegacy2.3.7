
















// Generated on 10/13/2017 02:19:05
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PrismAlignmentBonusResultMessage : Message
{

public const uint Id = 5842;
public override uint MessageId
{
    get { return Id; }
}

public Types.AlignmentBonusInformations alignmentBonus;
        

public PrismAlignmentBonusResultMessage()
{
}

public PrismAlignmentBonusResultMessage(Types.AlignmentBonusInformations alignmentBonus)
        {
            this.alignmentBonus = alignmentBonus;
        }
        

public override void Serialize(IDataWriter writer)
{

alignmentBonus.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

alignmentBonus = new Types.AlignmentBonusInformations();
            alignmentBonus.Deserialize(reader);
            

}


}


}
