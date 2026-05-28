
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GameRolePlayGroupMonsterInformations : GameRolePlayActorInformations
{

public const short Id = 160;
public override short TypeId
{
    get { return Id; }
}

public int mainCreatureGenericId;
        public sbyte mainCreatureGrade;
        public IEnumerable<Types.MonsterInGroupInformations> underlings;
        public short ageBonus;
        public sbyte alignmentSide;
        

public GameRolePlayGroupMonsterInformations()
{
}

public GameRolePlayGroupMonsterInformations(int contextualId, Types.EntityLook look, Types.EntityDispositionInformations disposition, int mainCreatureGenericId, sbyte mainCreatureGrade, IEnumerable<Types.MonsterInGroupInformations> underlings, short ageBonus, sbyte alignmentSide)
         : base(contextualId, look, disposition)
        {
            this.mainCreatureGenericId = mainCreatureGenericId;
            this.mainCreatureGrade = mainCreatureGrade;
            this.underlings = underlings;
            this.ageBonus = ageBonus;
            this.alignmentSide = alignmentSide;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(mainCreatureGenericId);
            writer.WriteSByte(mainCreatureGrade);
            var underlings_before = writer.Position;
            var underlings_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in underlings)
            {
                 entry.Serialize(writer);
                 underlings_count++;
            }
            var underlings_after = writer.Position;
            writer.Seek((int)underlings_before);
            writer.WriteUShort((ushort)underlings_count);
            writer.Seek((int)underlings_after);

            writer.WriteShort(ageBonus);
            writer.WriteSByte(alignmentSide);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            mainCreatureGenericId = reader.ReadInt();
            mainCreatureGrade = reader.ReadSByte();
            if (mainCreatureGrade < 0)
                throw new Exception("Forbidden value on mainCreatureGrade = " + mainCreatureGrade + ", it doesn't respect the following condition : mainCreatureGrade < 0");
            var limit = reader.ReadUShort();
            var underlings_ = new Types.MonsterInGroupInformations[limit];
            for (int i = 0; i < limit; i++)
            {
                 underlings_[i] = new Types.MonsterInGroupInformations();
                 underlings_[i].Deserialize(reader);
            }
            underlings = underlings_;
            ageBonus = reader.ReadShort();
            if (ageBonus < -1 || ageBonus > 1000)
                throw new Exception("Forbidden value on ageBonus = " + ageBonus + ", it doesn't respect the following condition : ageBonus < -1 || ageBonus > 1000");
            alignmentSide = reader.ReadSByte();
            

}



}


}
