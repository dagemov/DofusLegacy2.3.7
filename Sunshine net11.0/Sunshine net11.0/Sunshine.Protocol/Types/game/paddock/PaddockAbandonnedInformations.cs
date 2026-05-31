
















// Generated on 10/13/2017 02:17:27
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class PaddockAbandonnedInformations : PaddockBuyableInformations
{

public const short Id = 133;
public override short TypeId
{
    get { return Id; }
}

public int guildId;
        

public PaddockAbandonnedInformations()
{
}

public PaddockAbandonnedInformations(short maxOutdoorMount, short maxItems, int price, bool locked, int guildId)
         : base(maxOutdoorMount, maxItems, price, locked)
        {
            this.guildId = guildId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(guildId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            guildId = reader.ReadInt();
            

}



}


}
