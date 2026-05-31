
















// Generated on 10/13/2017 02:17:23
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GameFightMinimalStatsPreparation : GameFightMinimalStats
{

public const short Id = 360;
public override short TypeId
{
    get { return Id; }
}

public int initiative;
        

public GameFightMinimalStatsPreparation()
{
}

public GameFightMinimalStatsPreparation(int lifePoints, int maxLifePoints, int baseMaxLifePoints, int permanentDamagePercent, int shieldPoints, short actionPoints, short movementPoints, int summoner, short neutralElementResistPercent, short earthElementResistPercent, short waterElementResistPercent, short airElementResistPercent, short fireElementResistPercent, short dodgePALostProbability, short dodgePMLostProbability, short tackleBlock, short tackleEvade, sbyte invisibilityState, int initiative)
         : base(lifePoints, maxLifePoints, baseMaxLifePoints, permanentDamagePercent, shieldPoints, actionPoints, movementPoints, summoner, neutralElementResistPercent, earthElementResistPercent, waterElementResistPercent, airElementResistPercent, fireElementResistPercent, dodgePALostProbability, dodgePMLostProbability, tackleBlock, tackleEvade, invisibilityState)
        {
            this.initiative = initiative;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(initiative);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            initiative = reader.ReadInt();
            if (initiative < 0)
                throw new Exception("Forbidden value on initiative = " + initiative + ", it doesn't respect the following condition : initiative < 0");
            

}



}


}
