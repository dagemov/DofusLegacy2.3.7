
















// Generated on 10/13/2017 02:18:48
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MapComplementaryInformationsDataInHouseMessage : MapComplementaryInformationsDataMessage
{

public const uint Id = 6130;
public override uint MessageId
{
    get { return Id; }
}

public Types.HouseInformationsInside currentHouse;
        

public MapComplementaryInformationsDataInHouseMessage()
{
}

public MapComplementaryInformationsDataInHouseMessage(short subareaId, int mapId, sbyte subareaAlignmentSide, IEnumerable<Types.HouseInformations> houses, IEnumerable<Types.GameRolePlayActorInformations> actors, IEnumerable<Types.InteractiveElement> interactiveElements, IEnumerable<Types.StatedElement> statedElements, IEnumerable<Types.MapObstacle> obstacles, IEnumerable<Types.FightCommonInformations> fights, Types.HouseInformationsInside currentHouse)
         : base(subareaId, mapId, subareaAlignmentSide, houses, actors, interactiveElements, statedElements, obstacles, fights)
        {
            this.currentHouse = currentHouse;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            currentHouse.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            currentHouse = new HouseInformationsInside();
            currentHouse.Deserialize(reader);
            

}


}


}
