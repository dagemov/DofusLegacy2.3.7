
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class HumanInformations
{

public const short Id = 157;
public virtual short TypeId
{
    get { return Id; }
}

public IEnumerable<Types.EntityLook> followingCharactersLook;
        public sbyte emoteId;
        public ushort emoteEndTime;
        public Types.ActorRestrictionsInformations restrictions;
        public short titleId;
        public string titleParam;
        

public HumanInformations()
{
}

public HumanInformations(IEnumerable<Types.EntityLook> followingCharactersLook, sbyte emoteId, ushort emoteEndTime, Types.ActorRestrictionsInformations restrictions, short titleId, string titleParam)
        {
            this.followingCharactersLook = followingCharactersLook;
            this.emoteId = emoteId;
            this.emoteEndTime = emoteEndTime;
            this.restrictions = restrictions;
            this.titleId = titleId;
            this.titleParam = titleParam;
        }
        

public virtual void Serialize(IDataWriter writer)
{

var followingCharactersLook_before = writer.Position;
            var followingCharactersLook_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in followingCharactersLook)
            {
                 entry.Serialize(writer);
                 followingCharactersLook_count++;
            }
            var followingCharactersLook_after = writer.Position;
            writer.Seek((int)followingCharactersLook_before);
            writer.WriteUShort((ushort)followingCharactersLook_count);
            writer.Seek((int)followingCharactersLook_after);

            writer.WriteSByte(emoteId);
            writer.WriteUShort(emoteEndTime);
            restrictions.Serialize(writer);
            writer.WriteShort(titleId);
            writer.WriteUTF(titleParam);
            

}

public virtual void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var followingCharactersLook_ = new Types.EntityLook[limit];
            for (int i = 0; i < limit; i++)
            {
                 followingCharactersLook_[i] = new Types.EntityLook();
                 followingCharactersLook_[i].Deserialize(reader);
            }
            followingCharactersLook = followingCharactersLook_;
            emoteId = reader.ReadSByte();
            emoteEndTime = reader.ReadUShort();
            if (emoteEndTime < 0 || emoteEndTime > 65535)
                throw new Exception("Forbidden value on emoteEndTime = " + emoteEndTime + ", it doesn't respect the following condition : emoteEndTime < 0 || emoteEndTime > 65535");
            restrictions = new Types.ActorRestrictionsInformations();
            restrictions.Deserialize(reader);
            titleId = reader.ReadShort();
            if (titleId < 0)
                throw new Exception("Forbidden value on titleId = " + titleId + ", it doesn't respect the following condition : titleId < 0");
            titleParam = reader.ReadUTF();
            

}



}


}
