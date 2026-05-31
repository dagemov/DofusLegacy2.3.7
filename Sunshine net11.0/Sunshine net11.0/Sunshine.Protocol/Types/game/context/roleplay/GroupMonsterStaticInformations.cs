
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GroupMonsterStaticInformations
{

public const short Id = 140;
public virtual short TypeId
{
    get { return Id; }
}

public int mainCreatureGenericId;
        public sbyte mainCreatureGrade;
        public IEnumerable<Types.MonsterInGroupInformations> underlings;
        

public GroupMonsterStaticInformations()
{
}

public GroupMonsterStaticInformations(int mainCreatureGenericId, sbyte mainCreatureGrade, IEnumerable<Types.MonsterInGroupInformations> underlings)
        {
            this.mainCreatureGenericId = mainCreatureGenericId;
            this.mainCreatureGrade = mainCreatureGrade;
            this.underlings = underlings;
        }
        

public virtual void Serialize(IDataWriter writer)
{

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

            

}

public virtual void Deserialize(IDataReader reader)
{

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
            

}



}


}
